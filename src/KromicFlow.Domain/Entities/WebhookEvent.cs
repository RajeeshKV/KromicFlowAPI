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
}
