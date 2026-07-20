using System.Text.Json;
using KromicFlow.Application.Abstractions;
using KromicFlow.Application.Common;
using KromicFlow.Domain.Entities;
using KromicFlow.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace KromicFlow.Application.Features.Webhooks.PersistMetaWebhook;

internal sealed class PersistMetaWebhookCommandHandler(
    IKromicFlowDbContext db,
    IWebhookExecutor webhookExecutor,
    ILogger<PersistMetaWebhookCommandHandler> logger) : IRequestHandler<PersistMetaWebhookCommand, Result>
{
    public async Task<Result> Handle(PersistMetaWebhookCommand request, CancellationToken cancellationToken)
    {
        logger.LogInformation("Processing webhook event {EventId}", request.EventId);

        // Idempotency check — Meta retries on non-200 so duplicates are common
        if (await db.WebhookEvents.AnyAsync(x => x.EventId == request.EventId, cancellationToken))
        {
            logger.LogInformation("Webhook event {EventId} already seen, skipping", request.EventId);
            return Result.Success();
        }

        var parsed = ExtractWebhookIds(request.Payload);
        logger.LogInformation("Extracted IDs — InstagramAccountId: {AccountId}, InstagramMediaId: {MediaId}",
            parsed.InstagramAccountId ?? "null", parsed.InstagramMediaId ?? "null");

        // Unknown account — persist for debugging only, do not attempt execution
        if (string.IsNullOrEmpty(parsed.InstagramAccountId))
        {
            logger.LogWarning("Cannot determine Instagram account from payload, persisting for debug");
            db.WebhookEvents.Add(new WebhookEvent
            {
                EventId = request.EventId,
                Payload = request.Payload,
                Status = WebhookStatus.Skipped,
                FailureReason = "Could not extract InstagramAccountId from payload"
            });
            await db.SaveChangesAsync(cancellationToken);
            return Result.Success();
        }

        var account = await db.InstagramAccounts
            .FirstOrDefaultAsync(x => x.InstagramUserId == parsed.InstagramAccountId, cancellationToken);

        if (account is null)
        {
            logger.LogWarning("Instagram account {AccountId} not in system, ignoring", parsed.InstagramAccountId);
            return Result.Success();
        }

        if (!account.IsConnected)
        {
            logger.LogWarning("Instagram account {AccountId} is disconnected, persisting for audit", account.Id);
            db.WebhookEvents.Add(new WebhookEvent
            {
                EventId = request.EventId,
                Payload = request.Payload,
                Status = WebhookStatus.Skipped,
                FailureReason = "Account is disconnected"
            });
            await db.SaveChangesAsync(cancellationToken);
            return Result.Success();
        }

        // Persist as Pending first so we have a durable record even if execution crashes
        var webhookEvent = new WebhookEvent
        {
            EventId = request.EventId,
            Payload = request.Payload,
            Status = WebhookStatus.Pending
        };
        db.WebhookEvents.Add(webhookEvent);
        await db.SaveChangesAsync(cancellationToken);

        logger.LogInformation("Webhook event {EventId} saved, executing immediately", request.EventId);

        // Execute inline — zero delay on the happy path.
        // If this throws, the event stays Pending and the retry sweeper picks it up.
        await webhookExecutor.ExecuteAsync(webhookEvent, cancellationToken);
        await db.SaveChangesAsync(cancellationToken);

        logger.LogInformation("Webhook event {EventId} finished with status {Status}", request.EventId, webhookEvent.Status);
        return Result.Success();
    }

    private static (string? InstagramAccountId, string? InstagramMediaId) ExtractWebhookIds(string payload)
    {
        try
        {
            using var doc = JsonDocument.Parse(payload);
            var root = doc.RootElement;

            string? instagramAccountId = null;
            string? instagramMediaId = null;

            if (root.TryGetProperty("entry", out var entries) && entries.GetArrayLength() > 0)
            {
                var entry = entries[0];

                if (entry.TryGetProperty("id", out var entryId))
                    instagramAccountId = entryId.GetString();

                if (entry.TryGetProperty("changes", out var changes) && changes.GetArrayLength() > 0)
                {
                    var value = changes[0].TryGetProperty("value", out var v) ? v : default;

                    if (value.ValueKind == System.Text.Json.JsonValueKind.Object)
                    {
                        if (value.TryGetProperty("media", out var media) && media.TryGetProperty("id", out var mid))
                            instagramMediaId = mid.GetString();

                        if (instagramMediaId is null && value.TryGetProperty("media_id", out var mid2))
                            instagramMediaId = mid2.GetString();
                    }
                }
            }

            return (instagramAccountId, instagramMediaId);
        }
        catch
        {
            return (null, null);
        }
    }
}
