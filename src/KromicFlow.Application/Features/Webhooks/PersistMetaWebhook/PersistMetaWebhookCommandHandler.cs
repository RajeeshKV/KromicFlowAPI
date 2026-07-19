using KromicFlow.Application.Abstractions;
using KromicFlow.Application.Common;
using KromicFlow.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace KromicFlow.Application.Features.Webhooks.PersistMetaWebhook;

internal sealed class PersistMetaWebhookCommandHandler(IKromicFlowDbContext db) : IRequestHandler<PersistMetaWebhookCommand, Result>
{
    public async Task<Result> Handle(PersistMetaWebhookCommand request, CancellationToken cancellationToken)
    {
        if (await db.WebhookEvents.AnyAsync(x => x.EventId == request.EventId, cancellationToken)) return Result.Success();

        // Parse webhook payload to extract Instagram account ID
        var instagramAccountId = ExtractInstagramAccountId(request.Payload);
        if (string.IsNullOrEmpty(instagramAccountId))
        {
            // Cannot determine account, but still persist for debugging
            db.WebhookEvents.Add(new WebhookEvent { EventId = request.EventId, Payload = request.Payload });
            await db.SaveChangesAsync(cancellationToken);
            return Result.Success();
        }

        // Look up Instagram account and check connection state
        var instagramAccount = await db.InstagramAccounts
            .FirstOrDefaultAsync(x => x.InstagramUserId == instagramAccountId, cancellationToken);

        if (instagramAccount == null)
        {
            // Account not found in our system, ignore webhook
            return Result.Success();
        }

        // Only process webhooks for connected accounts
        if (!instagramAccount.IsConnected)
        {
            // Account is disconnected, ignore webhook but still persist for audit
            db.WebhookEvents.Add(new WebhookEvent { EventId = request.EventId, Payload = request.Payload });
            await db.SaveChangesAsync(cancellationToken);
            return Result.Success();
        }

        // Account is connected, persist webhook for processing
        db.WebhookEvents.Add(new WebhookEvent { EventId = request.EventId, Payload = request.Payload });
        await db.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }

    private static string? ExtractInstagramAccountId(string payload)
    {
        try
        {
            using var doc = JsonDocument.Parse(payload);
            var root = doc.RootElement;

            // Try to extract Instagram account ID from various webhook formats
            // Format 1: comments webhook
            if (root.TryGetProperty("entry", out var entries) && entries.GetArrayLength() > 0)
            {
                var entry = entries[0];
                if (entry.TryGetProperty("changes", out var changes) && changes.GetArrayLength() > 0)
                {
                    var change = changes[0];
                    if (change.TryGetProperty("value", out var value))
                    {
                        if (value.TryGetProperty("from", out var from) && from.TryGetProperty("id", out var id))
                        {
                            return id.GetString();
                        }
                        // For comments, the Instagram account might be in different field
                        if (value.TryGetProperty("instagram_user_id", out var igUserId))
                        {
                            return igUserId.GetString();
                        }
                    }
                }
            }

            return null;
        }
        catch
        {
            return null;
        }
    }
}
