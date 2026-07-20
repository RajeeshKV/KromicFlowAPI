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

        // Primary dedup on EventId (payload SHA256) — catches exact retries
        if (await db.WebhookEvents.AnyAsync(x => x.EventId == request.EventId, cancellationToken))
        {
            logger.LogInformation("Webhook event {EventId} already seen (exact duplicate), skipping", request.EventId);
            return Result.Success();
        }

        var parsed = ExtractWebhookIds(request.Payload);
        logger.LogInformation("Extracted IDs — InstagramAccountId: {AccountId}, MediaId: {MediaId}, CommentId: {CommentId}",
            parsed.InstagramAccountId ?? "null", parsed.InstagramMediaId ?? "null", parsed.CommentId ?? "null");

        // Secondary dedup on CommentId — Meta sometimes retries with a slightly different
        // payload (different timestamp) which produces a different SHA256 but is the same comment
        if (!string.IsNullOrEmpty(parsed.CommentId))
        {
            if (await db.WebhookEvents.AnyAsync(x => x.CommentId == parsed.CommentId, cancellationToken))
            {
                logger.LogInformation("Comment {CommentId} already processed, skipping duplicate webhook", parsed.CommentId);
                return Result.Success();
            }
        }

        // Unknown account — persist for debugging only
        if (string.IsNullOrEmpty(parsed.InstagramAccountId))
        {
            logger.LogWarning("Cannot determine Instagram account from payload, persisting for debug");
            db.WebhookEvents.Add(new WebhookEvent
            {
                EventId = request.EventId,
                Payload = request.Payload,
                CommentId = parsed.CommentId,
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
                CommentId = parsed.CommentId,
                Status = WebhookStatus.Skipped,
                FailureReason = "Account is disconnected",
                InstagramAccountId = account.Id
            });
            await db.SaveChangesAsync(cancellationToken);
            return Result.Success();
        }

        // Persist as Pending — durable record before execution
        var webhookEvent = new WebhookEvent
        {
            EventId = request.EventId,
            Payload = request.Payload,
            CommentId = parsed.CommentId,
            Status = WebhookStatus.Pending,
            InstagramAccountId = account.Id
        };
        db.WebhookEvents.Add(webhookEvent);
        await db.SaveChangesAsync(cancellationToken);

        logger.LogInformation("Webhook event {EventId} saved, executing immediately", request.EventId);

        await webhookExecutor.ExecuteAsync(webhookEvent, cancellationToken);
        await db.SaveChangesAsync(cancellationToken);

        logger.LogInformation("Webhook event {EventId} finished with status {Status}", request.EventId, webhookEvent.Status);
        return Result.Success();
    }

    private static (string? InstagramAccountId, string? InstagramMediaId, string? CommentId) ExtractWebhookIds(string payload)
    {
        try
        {
            using var doc = JsonDocument.Parse(payload);
            var root = doc.RootElement;

            string? instagramAccountId = null;
            string? instagramMediaId = null;
            string? commentId = null;

            if (root.TryGetProperty("entry", out var entries) && entries.GetArrayLength() > 0)
            {
                var entry = entries[0];

                if (entry.TryGetProperty("id", out var entryId))
                    instagramAccountId = entryId.GetString();

                if (entry.TryGetProperty("changes", out var changes) && changes.GetArrayLength() > 0)
                {
                    var value = changes[0].TryGetProperty("value", out var v) ? v : default;

                    if (value.ValueKind == JsonValueKind.Object)
                    {
                        if (value.TryGetProperty("id", out var cid))
                            commentId = cid.GetString();

                        if (value.TryGetProperty("media", out var media) && media.TryGetProperty("id", out var mid))
                            instagramMediaId = mid.GetString();

                        if (instagramMediaId is null && value.TryGetProperty("media_id", out var mid2))
                            instagramMediaId = mid2.GetString();
                    }
                }
            }

            return (instagramAccountId, instagramMediaId, commentId);
        }
        catch
        {
            return (null, null, null);
        }
    }
}
