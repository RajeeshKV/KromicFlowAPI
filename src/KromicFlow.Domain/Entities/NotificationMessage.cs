using KromicFlow.Domain.Enums;

namespace KromicFlow.Domain.Entities;

public sealed class NotificationMessage : Entity
{
    public Guid? UserId { get; set; }
    public User? User { get; set; }
    public NotificationAudience Audience { get; set; }
    public NotificationChannel Channel { get; set; }
    public NotificationStatus Status { get; set; } = NotificationStatus.Pending;
    public string Subject { get; set; } = string.Empty;
    public string Body { get; set; } = string.Empty;
    public string? ProviderMessageId { get; set; }
    public string? FailureReason { get; set; }
    public DateTime? SentUtc { get; set; }
}
