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
        var accountIds = await db.InstagramAccounts
            .Where(x => x.UserId == request.UserId)
            .Select(x => x.Id)
            .ToListAsync(cancellationToken);

        if (request.InstagramAccountId.HasValue)
            accountIds = accountIds.Where(id => id == request.InstagramAccountId.Value).ToList();

        var automationQuery = db.Automations.Where(x => accountIds.Contains(x.InstagramAccountId));
        var totalAutomations = await automationQuery.CountAsync(cancellationToken);
        var activeAutomations = await automationQuery.CountAsync(x => x.Enabled, cancellationToken);

        var eventsQuery = db.WebhookEvents
            .Where(x => x.InstagramAccountId.HasValue && accountIds.Contains(x.InstagramAccountId.Value));

        // Apply optional date range
        if (request.From.HasValue)
            eventsQuery = eventsQuery.Where(x => x.ReceivedUtc >= request.From.Value);
        if (request.To.HasValue)
            eventsQuery = eventsQuery.Where(x => x.ReceivedUtc <= request.To.Value);

        var today = DateTime.UtcNow.Date;

        var executionsToday = await db.WebhookEvents
            .Where(x => x.InstagramAccountId.HasValue && accountIds.Contains(x.InstagramAccountId.Value))
            .CountAsync(x => x.ReceivedUtc >= today &&
                             (x.Status == WebhookStatus.Completed || x.Status == WebhookStatus.Failed),
                cancellationToken);

        var totalExecutions  = await eventsQuery.CountAsync(x => x.Status == WebhookStatus.Completed || x.Status == WebhookStatus.Failed, cancellationToken);
        var successCount     = await eventsQuery.CountAsync(x => x.Status == WebhookStatus.Completed, cancellationToken);
        var failedCount      = await eventsQuery.CountAsync(x => x.Status == WebhookStatus.Failed, cancellationToken);
        var skippedCount     = await eventsQuery.CountAsync(x => x.Status == WebhookStatus.Skipped, cancellationToken);
        var publicReplies    = await eventsQuery.CountAsync(x => x.PublicReplySentUtc.HasValue, cancellationToken);
        var privateReplies   = await eventsQuery.CountAsync(x => x.PrivateReplySentUtc.HasValue, cancellationToken);

        var successRate = totalExecutions > 0
            ? Math.Round((double)successCount / totalExecutions * 100, 1)
            : 0.0;

        return new DashboardStatsDto(
            ActiveAutomations: activeAutomations,
            TotalAutomations: totalAutomations,
            ExecutionsToday: executionsToday,
            TotalExecutions: totalExecutions,
            PublicRepliesSent: publicReplies,
            PrivateRepliesSent: privateReplies,
            SuccessCount: successCount,
            FailedCount: failedCount,
            SkippedCount: skippedCount,
            SuccessRate: successRate
        );
    }
}
