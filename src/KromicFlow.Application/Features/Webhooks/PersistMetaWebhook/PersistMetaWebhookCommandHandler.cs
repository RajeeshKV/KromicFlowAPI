using KromicFlow.Application.Abstractions;
using KromicFlow.Application.Common;
using KromicFlow.Application.Services;
using KromicFlow.Domain.Entities;
using KromicFlow.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace KromicFlow.Application.Features.Webhooks.PersistMetaWebhook;

internal sealed class PersistMetaWebhookCommandHandler(IKromicFlowDbContext db, IAutomationScopeService automationScopeService) : IRequestHandler<PersistMetaWebhookCommand, Result>
{
    public async Task<Result> Handle(PersistMetaWebhookCommand request, CancellationToken cancellationToken)
    {
        if (await db.WebhookEvents.AnyAsync(x => x.EventId == request.EventId, cancellationToken)) return Result.Success();

        // Parse webhook payload to extract Instagram account ID and media ID
        var (instagramAccountId, instagramMediaId) = ExtractWebhookIds(request.Payload);
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

        // If media ID is present, validate automation scope
        if (!string.IsNullOrEmpty(instagramMediaId))
        {
            // Get all enabled automations for this account
            var automations = await db.Automations
                .Where(x => x.InstagramAccountId == instagramAccount.Id && x.Enabled)
                .Select(x => x.Id)
                .ToListAsync(cancellationToken);

            // Check if any automation is applicable for this media
            bool hasApplicableAutomation = false;
            foreach (var automationId in automations)
            {
                if (await automationScopeService.IsAutomationApplicableAsync(automationId, instagramMediaId, cancellationToken))
                {
                    hasApplicableAutomation = true;
                    break;
                }
            }

            // If no automation is applicable, still persist for audit but mark as skipped
            if (!hasApplicableAutomation)
            {
                db.WebhookEvents.Add(new WebhookEvent 
                { 
                    EventId = request.EventId, 
                    Payload = request.Payload,
                    Status = WebhookStatus.Skipped,
                    FailureReason = "No applicable automation for media scope"
                });
                await db.SaveChangesAsync(cancellationToken);
                return Result.Success();
            }
        }

        // Account is connected and automation is applicable, persist webhook for processing
        db.WebhookEvents.Add(new WebhookEvent { EventId = request.EventId, Payload = request.Payload });
        await db.SaveChangesAsync(cancellationToken);
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

            // Try to extract IDs from various webhook formats
            // Format 1: comments webhook
            if (root.TryGetProperty("entry", out var entries) && entries.GetArrayLength() > 0)
            {
                var entry = entries[0];
                if (entry.TryGetProperty("changes", out var changes) && changes.GetArrayLength() > 0)
                {
                    var change = changes[0];
                    if (change.TryGetProperty("value", out var value))
                    {
                        // Extract Instagram account ID
                        if (value.TryGetProperty("from", out var from) && from.TryGetProperty("id", out var id))
                        {
                            instagramAccountId = id.GetString();
                        }
                        if (value.TryGetProperty("instagram_user_id", out var igUserId))
                        {
                            instagramAccountId = igUserId.GetString();
                        }
                        
                        // Extract media ID
                        if (value.TryGetProperty("media", out var media))
                        {
                            if (media.TryGetProperty("id", out var mediaId))
                            {
                                instagramMediaId = mediaId.GetString();
                            }
                        }
                        if (value.TryGetProperty("media_id", out var mediaId2))
                        {
                            instagramMediaId = mediaId2.GetString();
                        }
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
