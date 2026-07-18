using KromicFlow.Application.Abstractions;
using KromicFlow.Application.Options;
using KromicFlow.Infrastructure.Persistence;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace KromicFlow.Api.Controllers;
[ApiController]
[Route("api/health")]
public sealed class HealthController(
    KromicFlowDbContext dbContext,
    IOptions<MetaOptions> metaOptions) : ControllerBase
{
    [HttpGet]
    public async Task<HealthDto> Get(CancellationToken cancellationToken)
    {
        var databaseStatus = await CheckDatabaseAsync(cancellationToken);
        var metaConfigStatus = CheckMetaConfiguration();
        var pendingOutboxEvents = await GetPendingOutboxEventsCountAsync(cancellationToken);
        
        var overallStatus = (databaseStatus == "Connected" && metaConfigStatus == "Configured") ? "Healthy" : "Unhealthy";
        
        return new HealthDto(
            overallStatus,
            databaseStatus,
            metaConfigStatus,
            "Running",
            pendingOutboxEvents,
            DateTimeOffset.UtcNow);
    }

    [HttpHead]
    public async Task<IActionResult> Head(CancellationToken cancellationToken)
    {
        await dbContext.Database.ExecuteSqlRawAsync("SELECT 1", cancellationToken);
        return Ok();
    }

    private async Task<string> CheckDatabaseAsync(CancellationToken cancellationToken)
    {
        try
        {
            await dbContext.Database.ExecuteSqlRawAsync("SELECT 1", cancellationToken);
            return "Connected";
        }
        catch
        {
            return "Disconnected";
        }
    }

    private string CheckMetaConfiguration()
    {
        var meta = metaOptions.Value;
        if (!string.IsNullOrWhiteSpace(meta.AppId) && 
            !string.IsNullOrWhiteSpace(meta.AppSecret) &&
            !string.IsNullOrWhiteSpace(meta.OAuthRedirectUri))
        {
            return "Configured";
        }
        return "Not Configured";
    }

    private async Task<int> GetPendingOutboxEventsCountAsync(CancellationToken cancellationToken)
    {
        return await dbContext.OutboxEvents
            .CountAsync(x => !x.ProcessedUtc.HasValue, cancellationToken);
    }
}

public sealed record HealthDto(
    string Status,
    string Database,
    string MetaConfig,
    string OutboxWorker,
    int PendingOutboxEvents,
    DateTimeOffset Utc);