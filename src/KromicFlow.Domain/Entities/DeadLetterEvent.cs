namespace KromicFlow.Domain.Entities;

public sealed class DeadLetterEvent : Entity
{
    public string EventType { get; set; } = string.Empty;
    public string Payload { get; set; } = string.Empty;
    public string? Error { get; set; }
    public DateTime FailedUtc { get; set; } = DateTime.UtcNow;
    public int RetryCount { get; set; }
}
