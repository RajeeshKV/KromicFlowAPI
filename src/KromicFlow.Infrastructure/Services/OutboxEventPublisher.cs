using System.Text.Json;
using KromicFlow.Application.Abstractions;
using Microsoft.EntityFrameworkCore;

namespace KromicFlow.Infrastructure.Services;

public sealed class OutboxEventPublisher(IKromicFlowDbContext db) : IOutboxEventPublisher
{
    public async Task PublishAsync(string eventType, object payload, CancellationToken cancellationToken = default)
    {
        var payloadJson = JsonSerializer.Serialize(payload);
        var outboxEvent = new Domain.Entities.OutboxEvent
        {
            EventType = eventType,
            Payload = payloadJson
        };
        db.OutboxEvents.Add(outboxEvent);
        await db.SaveChangesAsync(cancellationToken);
    }
}
