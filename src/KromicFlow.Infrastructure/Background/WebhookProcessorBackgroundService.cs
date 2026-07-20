using KromicFlow.Application.Abstractions;
using KromicFlow.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace KromicFlow.Infrastructure.Background;

/// <summary>
/// Retry sweeper — runs every 60 seconds and picks up WebhookEvents that are still
/// Pending after 60 seconds (i.e. the inline execution in the controller crashed or
/// the server restarted mid-flight). The happy path is handled immediately in
/// PersistMetaWebhookCommandHandler; this service is purely a safety net.
/// </summary>
public sealed class WebhookProcessorBackgroundService(
    IServiceScopeFactory scopeFactory,
    ILogger<WebhookProcessorBackgroundService> logger) : BackgroundService
{
    private static readonly TimeSpan SweepInterval = TimeSpan.FromSeconds(60);
    private static readonly TimeSpan StuckThreshold = TimeSpan.FromSeconds(60);
    private static readonly int BatchSize = 20;
    private static readonly int MaxRetries = 3;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("Webhook retry sweeper started (interval: {Interval}s, stuck threshold: {Threshold}s)",
            SweepInterval.TotalSeconds, StuckThreshold.TotalSeconds);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await SweepAsync(stoppingToken);
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Unhandled error in webhook retry sweeper");
            }

            await Task.Delay(SweepInterval, stoppingToken);
        }

        logger.LogInformation("Webhook retry sweeper stopped");
    }

    private async Task SweepAsync(CancellationToken cancellationToken)
    {
        using var scope = scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<IKromicFlowDbContext>();
        var executor = scope.ServiceProvider.GetRequiredService<IWebhookExecutor>();

        var stuckCutoff = DateTime.UtcNow - StuckThreshold;

        // Pick up events that are Pending (inline execution never ran or was interrupted)
        // and are old enough that we're sure the original request already completed/failed.
        var stuck = await db.WebhookEvents
            .Where(x =>
                x.Status == WebhookStatus.Pending &&
                x.RetryCount < MaxRetries &&
                x.ReceivedUtc < stuckCutoff)
            .OrderBy(x => x.ReceivedUtc)
            .Take(BatchSize)
            .ToListAsync(cancellationToken);

        if (stuck.Count == 0)
            return;

        logger.LogInformation("Retry sweeper found {Count} stuck webhook event(s)", stuck.Count);

        foreach (var webhookEvent in stuck)
        {
            logger.LogInformation("Retrying stuck webhook event {EventId} (retry #{Attempt})",
                webhookEvent.EventId, webhookEvent.RetryCount + 1);

            await executor.ExecuteAsync(webhookEvent, cancellationToken);
        }

        await db.SaveChangesAsync(cancellationToken);
        logger.LogInformation("Retry sweep complete");
    }
}
