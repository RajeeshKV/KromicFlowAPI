using KromicFlow.Application.Abstractions;
using KromicFlow.Application.Common;
using KromicFlow.Application.Services;
using KromicFlow.Domain.Entities;
using KromicFlow.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace KromicFlow.Application.Features.Webhooks.PersistMetaWebhook;

internal sealed class PersistMetaWebhookCommandHandler(
    IKromicFlowDbContext db, 
    IAutomationScopeService automationScopeService,
    ILogger<PersistMetaWebhookCommandHandler> logger) : IRequestHandler<PersistMetaWebhookCommand, Result>
{
    public async Task<Result> Handle(PersistMetaWebhookCommand request, CancellationToken cancellationToken)
    {
        logger.LogInformation("Processing webhook event {EventId}", request.EventId);
        
        if (await db.WebhookEvents.AnyAsync(x => x.EventId == request.EventId, cancellationToken))
        {
            logger.LogInformation("Webhook event {EventId} already processed, skipping", request.EventId);
            return Result.Success();
        }

        // Parse webhook payload to extract Instagram account ID and media ID
        logger.LogInformation("Extracting IDs from webhook payload");
        var (instagramAccountId, instagramMediaId) = ExtractWebhookIds(request.Payload);
        logger.LogInformation("Extracted IDs - InstagramAccountId: {InstagramAccountId}, InstagramMediaId: {InstagramMediaId}", 
            instagramAccountId ?? "null", instagramMediaId ?? "null");
        
        if (string.IsNullOrEmpty(instagramAccountId))
        {
            logger.LogWarning("Cannot determine Instagram account ID from webhook payload");
            // Cannot determine account, but still persist for debugging
            db.WebhookEvents.Add(new WebhookEvent { EventId = request.EventId, Payload = request.Payload });
            await db.SaveChangesAsync(cancellationToken);
            logger.LogInformation("Webhook persisted without account ID for debugging");
            return Result.Success();
        }

        // Look up Instagram account and check connection state
        logger.LogInformation("Looking up Instagram account {InstagramAccountId}", instagramAccountId);
        var instagramAccount = await db.InstagramAccounts
            .FirstOrDefaultAsync(x => x.InstagramUserId == instagramAccountId, cancellationToken);

        if (instagramAccount == null)
        {
            logger.LogWarning("Instagram account {InstagramAccountId} not found in system, ignoring webhook", instagramAccountId);
            // Account not found in our system, ignore webhook
            return Result.Success();
        }

        logger.LogInformation("Found Instagram account {AccountId} (IsConnected: {IsConnected})", instagramAccount.Id, instagramAccount.IsConnected);

        // Only process webhooks for connected accounts
        if (!instagramAccount.IsConnected)
        {
            logger.LogWarning("Instagram account {AccountId} is disconnected, ignoring webhook", instagramAccount.Id);
            // Account is disconnected, ignore webhook but still persist for audit
            db.WebhookEvents.Add(new WebhookEvent { EventId = request.EventId, Payload = request.Payload });
            await db.SaveChangesAsync(cancellationToken);
            logger.LogInformation("Webhook persisted for disconnected account");
            return Result.Success();
        }

        // If media ID is present, validate automation scope
        if (!string.IsNullOrEmpty(instagramMediaId))
        {
            logger.LogInformation("Media ID {InstagramMediaId} present, validating automation scope", instagramMediaId);
            
            // Get all enabled automations for this account
            var automations = await db.Automations
                .Where(x => x.InstagramAccountId == instagramAccount.Id && x.Enabled)
                .Select(x => x.Id)
                .ToListAsync(cancellationToken);

            logger.LogInformation("Found {Count} enabled automations for account {AccountId}", automations.Count, instagramAccount.Id);

            // Check if any automation is applicable for this media
            bool hasApplicableAutomation = false;
            foreach (var automationId in automations)
            {
                var isApplicable = await automationScopeService.IsAutomationApplicableAsync(automationId, instagramMediaId, cancellationToken);
                logger.LogInformation("Automation {AutomationId} applicable for media {MediaId}: {IsApplicable}", automationId, instagramMediaId, isApplicable);
                
                if (isApplicable)
                {
                    hasApplicableAutomation = true;
                    break;
                }
            }

            // If no automation is applicable, still persist for audit but mark as skipped
            if (!hasApplicableAutomation)
            {
                logger.LogWarning("No applicable automation found for media {MediaId}, marking webhook as skipped", instagramMediaId);
                db.WebhookEvents.Add(new WebhookEvent 
                { 
                    EventId = request.EventId, 
                    Payload = request.Payload,
                    Status = WebhookStatus.Skipped,
                    FailureReason = "No applicable automation for media scope"
                });
                await db.SaveChangesAsync(cancellationToken);
                logger.LogInformation("Webhook persisted with Skipped status");
                return Result.Success();
            }
            
            logger.LogInformation("Found applicable automation for media {MediaId}", instagramMediaId);
        }
        else
        {
            logger.LogInformation("No media ID in webhook, skipping scope validation");
        }

        // Account is connected and automation is applicable, persist webhook for processing
        logger.LogInformation("Persisting webhook for processing - Account: {AccountId}, Media: {MediaId}", 
            instagramAccount.Id, instagramMediaId ?? "none");
        
        db.WebhookEvents.Add(new WebhookEvent { EventId = request.EventId, Payload = request.Payload });
        await db.SaveChangesAsync(cancellationToken);
        
        logger.LogInformation("Webhook persisted successfully for event {EventId}", request.EventId);
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
        catch (Exception ex)
        {
            return (null, null);
        }
    }
}
