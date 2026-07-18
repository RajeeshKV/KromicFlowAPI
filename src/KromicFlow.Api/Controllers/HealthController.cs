using KromicFlow.Infrastructure.Persistence;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace KromicFlow.Api.Controllers;
[ApiController]
[Route("api/health")]
public sealed class HealthController(KromicFlowDbContext dbContext) : ControllerBase
{
    [HttpGet]
    public async Task<HealthDto> Get(CancellationToken cancellationToken)
    {
        await dbContext.Database.ExecuteSqlRawAsync("SELECT 1", cancellationToken);
        return new HealthDto("Healthy", "Connected", DateTimeOffset.UtcNow);
    }

    [HttpHead]
    public async Task<IActionResult> Head(CancellationToken cancellationToken)
    {
        await dbContext.Database.ExecuteSqlRawAsync("SELECT 1", cancellationToken);
        return Ok();
    }
}

public sealed record HealthDto(string Status, string Database, DateTimeOffset Utc);