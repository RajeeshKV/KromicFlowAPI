using System.Text.Json;
using KromicFlow.Application.Abstractions;
using KromicFlow.Domain.Entities;
using KromicFlow.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace KromicFlow.Infrastructure.Services;

public sealed class WebhookExecutor(
    IKromicFlowDbContext db,
    IMetaApiClient metaClient,
    IDataProtectionService dataProtection,
    ILogger<WebhookExecutor> logger) : IWebhookExecutor
{
    private static readonly int MaxRetries = 3;

    public async Task ExecuteAsync(WebhookEvent webhookEvent, CancellationToken cancellationToken)
    {
        logger.LogInformation("Executing webhook event {EventId}", webhookEvent.EventId);
        webhookEvent.Status = WebhookStatus.Processing;

        try
        {
            var parsed = ParseCommentPayload(webhookEvent.Payload);
            if (parsed is null)
            {
                logger.LogWarning("Webhook event {EventId} is not a comment payload, skipping", webhookEvent.EventId);
                webhookEvent.Status = WebhookStatus.Skipped;
                webhookEvent.FailureReason = "Unrecognised payload format";
                webhookEvent.ProcessedUtc = DateTime.UtcNow;
                return;
            }

            logger.LogInformation(
                "Comment — Account: {AccountIgId}, Media: {MediaId}, Comment: {CommentId}, From: {FromId} (@{Username}), ScopedId: {ScopedId}, Text: '{Text}'",
                parsed.AccountIgId, parsed.MediaId, parsed.CommentId,
                parsed.FromId, parsed.FromUsername, parsed.FromSelfIgScopedId ?? "none",
                parsed.CommentText);

            var account = await db.InstagramAccounts
                .FirstOrDefaultAsync(x => x.InstagramUserId == parsed.AccountIgId, cancellationToken);

            if (account is null || !account.IsConnected)
            {
                logger.LogWarning("Account {AccountIgId} not found or disconnected — skipping event {EventId}", parsed.AccountIgId, webhookEvent.EventId);
                webhookEvent.Status = WebhookStatus.Skipped;
                webhookEvent.FailureReason = "Account not found or disconnected";
                webhookEvent.ProcessedUtc = DateTime.UtcNow;
                return;
            }

            // Skip comments made by the account owner — prevents infinite reply loops
            if (parsed.FromId == account.InstagramUserId)
            {
                logger.LogInformation(
                    "Comment {CommentId} is from the account owner (@{Username}), skipping to prevent reply loop",
                    parsed.CommentId, parsed.FromUsername);
                webhookEvent.Status = WebhookStatus.Skipped;
                webhookEvent.FailureReason = "Own-account comment, skipped";
                webhookEvent.ProcessedUtc = DateTime.UtcNow;
                return;
            }

            var media = await db.InstagramMedia
                .FirstOrDefaultAsync(x => x.InstagramMediaId == parsed.MediaId && !x.IsDeleted, cancellationToken);

            if (media is null)
            {
                logger.LogWarning("Media {MediaId} not found in DB — skipping event {EventId}", parsed.MediaId, webhookEvent.EventId);
                webhookEvent.Status = WebhookStatus.Skipped;
                webhookEvent.FailureReason = $"Media {parsed.MediaId} not synced";
                webhookEvent.ProcessedUtc = DateTime.UtcNow;
                return;
            }

            var automations = await db.Automations
                .Include(x => x.AutomationMedia)
                .Where(x => x.InstagramAccountId == account.Id && x.Enabled)
                .OrderBy(x => x.Priority)
                .ToListAsync(cancellationToken);

            logger.LogInformation("Found {Count} enabled automation(s) for account {AccountId}", automations.Count, account.Id);

            var accessToken = dataProtection.Unprotect(account.AccessTokenEncrypted);
            bool anyFired = false;

            foreach (var automation in automations)
            {
                if (!IsInActiveWindow(automation))
                {
                    logger.LogDebug("Automation {AutomationId} outside active window, skipping", automation.Id);
                    continue;
                }

                if (!IsApplicableToMedia(automation, media))
                {
                    logger.LogDebug("Automation {AutomationId} scope does not cover media {MediaId}, skipping", automation.Id, parsed.MediaId);
                    continue;
                }

                if (!MatchesTrigger(automation, parsed.CommentText))
                {
                    logger.LogDebug("Automation {AutomationId} trigger did not match comment '{Text}', skipping", automation.Id, parsed.CommentText);
                    continue;
                }

                logger.LogInformation("Automation {AutomationId} matched — firing actions", automation.Id);

                // Public reply — only if the flag is enabled and not already sent
                if (automation.SendPublicReply && !string.IsNullOrWhiteSpace(automation.PublicReply))
                {
                    if (webhookEvent.PublicReplySentUtc.HasValue)
                    {
                        logger.LogInformation("Public reply already sent at {SentUtc}, skipping re-send", webhookEvent.PublicReplySentUtc);
                    }
                    else
                    {
                        logger.LogInformation("Posting public reply on comment {CommentId}", parsed.CommentId);
                        await metaClient.PostCommentReplyAsync(accessToken, parsed.CommentId, automation.PublicReply, cancellationToken);
                        webhookEvent.PublicReplySentUtc = DateTime.UtcNow;
                        logger.LogInformation("Public reply posted");
                    }
                }

                // Private reply — only if the flag is enabled and not already sent
                if (automation.SendPrivateReply && !string.IsNullOrWhiteSpace(automation.PrivateReply))
                {
                    if (webhookEvent.PrivateReplySentUtc.HasValue)
                    {
                        logger.LogInformation("Private reply already sent at {SentUtc}, skipping re-send", webhookEvent.PrivateReplySentUtc);
                    }
                    else
                    {
                        logger.LogInformation("Sending private reply for comment {CommentId}", parsed.CommentId);
                        await metaClient.SendPrivateReplyAsync(accessToken, account.InstagramUserId, parsed.CommentId, automation.PrivateReply, cancellationToken);
                        webhookEvent.PrivateReplySentUtc = DateTime.UtcNow;
                        logger.LogInformation("Private reply sent");
                    }
                }

                anyFired = true;
                break; // Fire only the first (highest-priority) matching automation
            }

            webhookEvent.Status = anyFired ? WebhookStatus.Completed : WebhookStatus.Skipped;
            webhookEvent.ProcessedUtc = DateTime.UtcNow;

            if (!anyFired)
                webhookEvent.FailureReason = "No matching automation for this trigger/scope";

            logger.LogInformation("Webhook event {EventId} completed with status {Status}", webhookEvent.EventId, webhookEvent.Status);
        }
        catch (Exception ex)
        {
            webhookEvent.RetryCount++;
            webhookEvent.FailureReason = ex.Message;

            if (webhookEvent.RetryCount >= MaxRetries)
            {
                webhookEvent.Status = WebhookStatus.Failed;
                logger.LogError(ex, "Webhook event {EventId} failed after {MaxRetries} attempts", webhookEvent.EventId, MaxRetries);
            }
            else
            {
                // Back to Pending — the retry sweeper picks it up.
                // PublicReplySentUtc / PrivateReplySentUtc are already set if those steps
                // succeeded, so they will be skipped on the next attempt.
                webhookEvent.Status = WebhookStatus.Pending;
                logger.LogWarning(ex, "Webhook event {EventId} failed (attempt {Attempt}/{MaxRetries}), will retry",
                    webhookEvent.EventId, webhookEvent.RetryCount, MaxRetries);
            }
        }
    }

    // ── Payload parsing ──────────────────────────────────────────────────────

    private sealed record CommentPayload(
        string AccountIgId,
        string MediaId,
        string CommentId,
        string FromId,               // from.id — IG_ID of commenter, used for self-check
        string? FromSelfIgScopedId,  // from.self_ig_scoped_id — IGSID required by messages API
        string FromUsername,
        string CommentText);

    private static CommentPayload? ParseCommentPayload(string payload)
    {
        try
        {
            using var doc = JsonDocument.Parse(payload);
            var root = doc.RootElement;

            if (!root.TryGetProperty("entry", out var entries) || entries.GetArrayLength() == 0)
                return null;

            var entry = entries[0];
            if (!entry.TryGetProperty("id", out var accountIdEl))
                return null;

            if (!entry.TryGetProperty("changes", out var changes) || changes.GetArrayLength() == 0)
                return null;

            var change = changes[0];
            if (!change.TryGetProperty("field", out var fieldEl) || fieldEl.GetString() != "comments")
                return null;

            if (!change.TryGetProperty("value", out var value))
                return null;

            var commentId = value.TryGetProperty("id", out var cid) ? cid.GetString() : null;
            var commentText = value.TryGetProperty("text", out var txt) ? txt.GetString() ?? string.Empty : string.Empty;

            string? mediaId = null;
            if (value.TryGetProperty("media", out var mediaEl) && mediaEl.TryGetProperty("id", out var mid))
                mediaId = mid.GetString();

            string? fromId = null;
            string? fromSelfIgScopedId = null;
            string? fromUsername = null;

            if (value.TryGetProperty("from", out var from))
            {
                if (from.TryGetProperty("id", out var fromIdEl))
                    fromId = fromIdEl.GetString();
                if (from.TryGetProperty("self_ig_scoped_id", out var scopedEl))
                    fromSelfIgScopedId = scopedEl.GetString();
                if (from.TryGetProperty("username", out var fromUsernameEl))
                    fromUsername = fromUsernameEl.GetString();
            }

            if (string.IsNullOrEmpty(commentId) || string.IsNullOrEmpty(mediaId) || string.IsNullOrEmpty(fromId))
                return null;

            return new CommentPayload(
                AccountIgId: accountIdEl.GetString()!,
                MediaId: mediaId,
                CommentId: commentId,
                FromId: fromId,
                FromSelfIgScopedId: fromSelfIgScopedId,
                FromUsername: fromUsername ?? string.Empty,
                CommentText: commentText);
        }
        catch
        {
            return null;
        }
    }

    // ── Automation matching ───────────────────────────────────────────────────

    private static bool IsInActiveWindow(Automation automation)
    {
        var now = DateTime.UtcNow;
        if (automation.ActiveFromUtc.HasValue && now < automation.ActiveFromUtc.Value) return false;
        if (automation.ActiveUntilUtc.HasValue && now > automation.ActiveUntilUtc.Value) return false;
        return true;
    }

    private static bool IsApplicableToMedia(Automation automation, InstagramMedia media)
    {
        return automation.Scope switch
        {
            AutomationScope.AllPosts      => true,
            AutomationScope.FuturePosts   => media.PostedAtUtc > automation.CreatedUtc,
            AutomationScope.ExistingPosts => media.PostedAtUtc <= automation.CreatedUtc,
            AutomationScope.SpecificPosts => automation.AutomationMedia.Any(x => x.InstagramMediaId == media.Id),
            _                             => false
        };
    }

    private static bool MatchesTrigger(Automation automation, string commentText)
    {
        if (automation.TriggerType == AutomationTriggerType.AnyComment)
            return true;

        if (string.IsNullOrWhiteSpace(automation.KeywordsJson))
            return false;

        try
        {
            var keywords = JsonSerializer.Deserialize<List<string>>(automation.KeywordsJson) ?? [];
            return keywords.Any(kw =>
                !string.IsNullOrWhiteSpace(kw) &&
                commentText.Contains(kw, StringComparison.OrdinalIgnoreCase));
        }
        catch
        {
            return false;
        }
    }
}
