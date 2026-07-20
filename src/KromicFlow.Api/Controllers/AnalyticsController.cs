using KromicFlow.Application.Features.Analytics.GetAutomationStats;
using KromicFlow.Application.Features.Analytics.GetConversations;
using KromicFlow.Application.Features.Analytics.GetDashboardStats;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace KromicFlow.Api.Controllers;

[Authorize]
[Route("api/v1/analytics")]
public sealed class AnalyticsController(IMediator mediator) : ApiControllerBase
{
    /// <summary>
    /// Dashboard-level stats for the current user.
    /// Pass instagramAccountId to scope to one account; omit for all accounts.
    /// </summary>
    [HttpGet("dashboard")]
    public async Task<IActionResult> Dashboard(
        [FromQuery] Guid? instagramAccountId,
        CancellationToken cancellationToken) =>
        Ok(await mediator.Send(new GetDashboardStatsQuery(User.GetSubjectId(), instagramAccountId), cancellationToken));

    /// <summary>
    /// Per-automation execution stats for a specific Instagram account.
    /// Used to populate the stats row on each automation card.
    /// </summary>
    [HttpGet("automations")]
    public async Task<IActionResult> AutomationStats(
        [FromQuery] Guid instagramAccountId,
        CancellationToken cancellationToken) =>
        Ok(await mediator.Send(new GetAutomationStatsQuery(User.GetSubjectId(), instagramAccountId), cancellationToken));

    /// <summary>
    /// Paginated list of unique commenters with their latest interaction.
    /// Used for the Conversations screen.
    /// </summary>
    [HttpGet("conversations")]
    public async Task<IActionResult> Conversations(
        [FromQuery] Guid instagramAccountId,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 25,
        CancellationToken cancellationToken = default) =>
        Ok(await mediator.Send(new GetConversationsQuery(User.GetSubjectId(), instagramAccountId, page, pageSize), cancellationToken));
}
