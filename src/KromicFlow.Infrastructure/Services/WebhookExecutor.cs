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
                "Comment — Account: {AccountIgId}, Media: {MediaId}, Comment: {CommentId}, From: {FromIgsid} (@{Username}), Text: '{Text}'",
                parsed.AccountIgId, parsed.MediaId, parsed.CommentId, parsed.FromIgsid, parsed.FromUsername, parsed.CommentText);

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

                if (!string.IsNullOrWhiteSpace(automation.PublicReply))
                {
                    logger.LogInformation("Posting public reply on comment {CommentId}", parsed.CommentId);
                    await metaClient.PostCommentReplyAsync(accessToken, parsed.CommentId, automation.PublicReply, cancellationToken);
                    logger.LogInformation("Public reply posted");
                }

                if (!string.IsNullOrWhiteSpace(automation.PrivateReply))
                {
                    logger.LogInformation("Sending DM to {FromIgsid}", parsed.FromIgsid);
                    await metaClient.SendDirectMessageAsync(accessToken, account.InstagramUserId, parsed.FromIgsid, automation.PrivateReply, cancellationToken);
                    logger.LogInformation("DM sent");
                }

                anyFired = true;
                break; // Fire the first (highest-priority) matching automation only
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
                // Back to Pending — the retry sweeper will pick it up
                webhookEvent.Status = WebhookStatus.Pending;
                logger.LogWarning(ex, "Webhook event {EventId} failed (attempt {Attempt}/{MaxRetries}), will retry", webhookEvent.EventId, webhookEvent.RetryCount, MaxRetries);
            }
        }
    }

    // ── Payload parsing ──────────────────────────────────────────────────────

    private sealed record CommentPayload(
        string AccountIgId,
        string MediaId,
        string CommentId,
        string FromIgsid,
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

            string? fromIgsid = null;
            string? fromUsername = null;
            if (value.TryGetProperty("from", out var from))
            {
                if (from.TryGetProperty("id", out var fromIdEl)) fromIgsid = fromIdEl.GetString();
                if (from.TryGetProperty("username", out var fromUsernameEl)) fromUsername = fromUsernameEl.GetString();
            }

            if (string.IsNullOrEmpty(commentId) || string.IsNullOrEmpty(mediaId) || string.IsNullOrEmpty(fromIgsid))
                return null;

            return new CommentPayload(
                AccountIgId: accountIdEl.GetString()!,
                MediaId: mediaId,
                CommentId: commentId,
                FromIgsid: fromIgsid,
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
