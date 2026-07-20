using KromicFlow.Application.Abstractions;
using KromicFlow.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace KromicFlow.Application.Features.Analytics.GetDashboardStats;

internal sealed class GetDashboardStatsQueryHandler(IKromicFlowDbContext db)
    : IRequestHandler<GetDashboardStatsQuery, DashboardStatsDto>
{
    public async Task<DashboardStatsDto> Handle(GetDashboardStatsQuery request, CancellationToken cancellationToken)
    {
        // Resolve which account IDs belong to this user
        var accountIds = await db.InstagramAccounts
            .Where(x => x.UserId == request.UserId)
            .Select(x => x.Id)
            .ToListAsync(cancellationToken);

        // Filter to a specific account if requested
        if (request.InstagramAccountId.HasValue)
            accountIds = accountIds.Where(id => id == request.InstagramAccountId.Value).ToList();

        // Automation counts
        var automationQuery = db.Automations
            .Where(x => accountIds.Contains(x.InstagramAccountId));

        var totalAutomations = await automationQuery.CountAsync(cancellationToken);
        var activeAutomations = await automationQuery.CountAsync(x => x.Enabled, cancellationToken);

        // Webhook event stats — only events linked to these accounts
        var eventsQuery = db.WebhookEvents
            .Where(x => x.InstagramAccountId.HasValue && accountIds.Contains(x.InstagramAccountId.Value));

        var today = DateTime.UtcNow.Date;

        var executionsToday = await eventsQuery
            .CountAsync(x => x.ReceivedUtc >= today &&
                             (x.Status == WebhookStatus.Completed || x.Status == WebhookStatus.Failed),
                cancellationToken);

        var totalExecutions = await eventsQuery
            .CountAsync(x => x.Status == WebhookStatus.Completed || x.Status == WebhookStatus.Failed,
                cancellationToken);

        var successCount = await eventsQuery
            .CountAsync(x => x.Status == WebhookStatus.Completed, cancellationToken);

        var failedCount = await eventsQuery
            .CountAsync(x => x.Status == WebhookStatus.Failed, cancellationToken);

        var skippedCount = await eventsQuery
            .CountAsync(x => x.Status == WebhookStatus.Skipped, cancellationToken);

        var publicRepliesSent = await eventsQuery
            .CountAsync(x => x.PublicReplySentUtc.HasValue, cancellationToken);

        var privateRepliesSent = await eventsQuery
            .CountAsync(x => x.PrivateReplySentUtc.HasValue, cancellationToken);

        var successRate = totalExecutions > 0
            ? Math.Round((double)successCount / totalExecutions * 100, 1)
            : 0.0;

        return new DashboardStatsDto(
            ActiveAutomations: activeAutomations,
            TotalAutomations: totalAutomations,
            ExecutionsToday: executionsToday,
            TotalExecutions: totalExecutions,
            PublicRepliesSent: publicRepliesSent,
            PrivateRepliesSent: privateRepliesSent,
            SuccessCount: successCount,
            FailedCount: failedCount,
            SkippedCount: skippedCount,
            SuccessRate: successRate
        );
    }
}
