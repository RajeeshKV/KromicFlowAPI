using KromicFlow.Application.Features.Analytics.GetAutomationStats;
using KromicFlow.Application.Features.Analytics.GetConversations;
using KromicFlow.Application.Features.Analytics.GetDashboardStats;
using KromicFlow.Application.Features.Analytics.GetTimeSeries;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace KromicFlow.Api.Controllers;

[Authorize]
[Route("api/v1/analytics")]
public sealed class AnalyticsController(IMediator mediator) : ApiControllerBase
{
    /// <summary>
    /// Account-level dashboard stats.
    /// Omit instagramAccountId to aggregate across all accounts.
    /// Optionally scope to a date range with from/to (ISO 8601 UTC).
    /// </summary>
    [HttpGet("dashboard")]
    public async Task<IActionResult> Dashboard(
        [FromQuery] Guid? instagramAccountId,
        [FromQuery] DateTime? from,
        [FromQuery] DateTime? to,
        CancellationToken cancellationToken) =>
        Ok(await mediator.Send(
            new GetDashboardStatsQuery(User.GetSubjectId(), instagramAccountId, from, to),
            cancellationToken));

    /// <summary>
    /// Per-automation execution stats for a specific Instagram account.
    /// Used to populate the stats row on each automation card.
    /// Optionally scope to a date range with from/to.
    /// </summary>
    [HttpGet("automations")]
    public async Task<IActionResult> AutomationStats(
        [FromQuery] Guid instagramAccountId,
        [FromQuery] DateTime? from,
        [FromQuery] DateTime? to,
        CancellationToken cancellationToken) =>
        Ok(await mediator.Send(
            new GetAutomationStatsQuery(User.GetSubjectId(), instagramAccountId, from, to),
            cancellationToken));

    /// <summary>
    /// Paginated list of unique commenters with their latest interaction.
    /// Used for the Conversations screen.
    /// Filter by mediaIgId to scope to a specific post.
    /// </summary>
    [HttpGet("conversations")]
    public async Task<IActionResult> Conversations(
        [FromQuery] Guid instagramAccountId,
        [FromQuery] string? mediaIgId,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 25,
        CancellationToken cancellationToken = default) =>
        Ok(await mediator.Send(
            new GetConversationsQuery(User.GetSubjectId(), instagramAccountId, mediaIgId, page, pageSize),
            cancellationToken));

    /// <summary>
    /// Daily execution counts for charting over a date range (max 90 days).
    /// Every day in the range is returned, zero-filled when no executions occurred.
    /// Optionally scope to a single automation with automationId.
    /// </summary>
    [HttpGet("timeseries")]
    public async Task<IActionResult> TimeSeries(
        [FromQuery] Guid instagramAccountId,
        [FromQuery] DateTime from,
        [FromQuery] DateTime to,
        [FromQuery] Guid? automationId,
        CancellationToken cancellationToken) =>
        Ok(await mediator.Send(
            new GetTimeSeriesQuery(User.GetSubjectId(), instagramAccountId, from, to, automationId),
            cancellationToken));
}
