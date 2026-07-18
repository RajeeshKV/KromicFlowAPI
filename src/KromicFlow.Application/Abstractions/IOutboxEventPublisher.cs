namespace KromicFlow.Application.Abstractions;

public interface IOutboxEventPublisher
{
    Task PublishAsync(string eventType, object payload, CancellationToken cancellationToken = default);
}
