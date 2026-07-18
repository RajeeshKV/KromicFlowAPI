namespace KromicFlow.Domain.Entities;

public sealed class OutboxEvent : Entity
{
    public string EventType { get; set; } = string.Empty;
    public string Payload { get; set; } = string.Empty;
    public DateTime? ProcessedUtc { get; set; }
    public string? Error { get; set; }
    public int RetryCount { get; set; }
    public bool IsProcessed => ProcessedUtc.HasValue;
}
