using KromicFlow.Domain.Enums;

namespace KromicFlow.Domain.Entities;

public sealed class WebhookEvent : Entity
{
    public string EventId { get; set; } = string.Empty;
    public string Payload { get; set; } = string.Empty;
    public WebhookStatus Status { get; set; } = WebhookStatus.Pending;
    public int RetryCount { get; set; }
    public DateTime ReceivedUtc { get; set; } = DateTime.UtcNow;
    public DateTime? ProcessedUtc { get; set; }
    public DateTime? PublicReplySentUtc { get; set; }   // set once public reply succeeds; prevents re-send on retry
    public DateTime? PrivateReplySentUtc { get; set; }  // set once DM succeeds; prevents re-send on retry
    public string? FailureReason { get; set; }

    // Analytics FKs — populated during processing for efficient querying
    public Guid? InstagramAccountId { get; set; }           // which account received the comment
    public InstagramAccount? InstagramAccount { get; set; }
    public Guid? AutomationId { get; set; }                 // which automation was fired (null if none matched)
    public Automation? Automation { get; set; }

    // Comment metadata — extracted from payload for conversation/analytics views
    public string? CommentId { get; set; }
    public string? CommentText { get; set; }
    public string? CommenterIgId { get; set; }
    public string? CommenterUsername { get; set; }
    public string? MediaIgId { get; set; }
}
