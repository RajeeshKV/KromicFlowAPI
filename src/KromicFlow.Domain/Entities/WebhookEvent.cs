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
    public string? FailureReason { get; set; }
}
