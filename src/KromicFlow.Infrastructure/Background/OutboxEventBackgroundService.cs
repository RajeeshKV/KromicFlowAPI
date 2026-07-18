using KromicFlow.Application.Abstractions;
using KromicFlow.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace KromicFlow.Infrastructure.Background;

public sealed class OutboxEventBackgroundService(
    IKromicFlowDbContext db,
    ILogger<OutboxEventBackgroundService> logger) : BackgroundService
{
    private static readonly TimeSpan ProcessingInterval = TimeSpan.FromSeconds(30);
    private static readonly TimeSpan MaxEventAge = TimeSpan.FromHours(1);
    private static readonly int MaxRetryCount = 5;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("Outbox Event Background Service started");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ProcessOutboxEventsAsync(stoppingToken);
                await Task.Delay(ProcessingInterval, stoppingToken);
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error in Outbox Event Background Service");
                await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken);
            }
        }

        logger.LogInformation("Outbox Event Background Service stopped");
    }

    private async Task ProcessOutboxEventsAsync(CancellationToken cancellationToken)
    {
        var unprocessedEvents = await db.OutboxEvents
            .Where(x => !x.IsProcessed && x.RetryCount < MaxRetryCount)
            .OrderBy(x => x.CreatedUtc)
            .Take(50)
            .ToListAsync(cancellationToken);

        if (unprocessedEvents.Count == 0)
        {
            return;
        }

        logger.LogInformation("Processing {Count} outbox events", unprocessedEvents.Count);

        foreach (var @event in unprocessedEvents)
        {
            try
            {
                await ProcessEventAsync(@event, cancellationToken);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to process outbox event {EventId} of type {EventType}", @event.Id, @event.EventType);
                @event.RetryCount++;
                @event.Error = ex.Message;
                
                if (@event.RetryCount >= MaxRetryCount)
                {
                    await MoveToDeadLetterAsync(@event, cancellationToken);
                    db.OutboxEvents.Remove(@event);
                }
            }
        }

        await db.SaveChangesAsync(cancellationToken);
    }

    private Task ProcessEventAsync(OutboxEvent @event, CancellationToken cancellationToken)
    {
        logger.LogInformation("Processing event {EventId} of type {EventType}", @event.Id, @event.EventType);
        
        // TODO: Implement actual event publishing logic based on event type
        // For now, we'll mark as processed
        @event.ProcessedUtc = DateTime.UtcNow;
        
        return Task.CompletedTask;
    }

    private async Task MoveToDeadLetterAsync(OutboxEvent @event, CancellationToken cancellationToken)
    {
        logger.LogWarning("Moving event {EventId} to dead letter queue after {RetryCount} retries", @event.Id, @event.RetryCount);
        
        var deadLetterEvent = new DeadLetterEvent
        {
            EventType = @event.EventType,
            Payload = @event.Payload,
            Error = @event.Error,
            FailedUtc = DateTime.UtcNow,
            RetryCount = @event.RetryCount
        };
        
        db.DeadLetterEvents.Add(deadLetterEvent);
        await db.SaveChangesAsync(cancellationToken);
    }
}
