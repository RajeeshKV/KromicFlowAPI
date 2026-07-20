using KromicFlow.Application.Common;
using MediatR;

namespace KromicFlow.Application.Features.Analytics.GetDashboardStats;

public sealed record GetDashboardStatsQuery(Guid UserId, Guid? InstagramAccountId) : IRequest<DashboardStatsDto>;

public sealed record DashboardStatsDto(
    int ActiveAutomations,
    int TotalAutomations,
    int ExecutionsToday,
    int TotalExecutions,
    int PublicRepliesSent,
    int PrivateRepliesSent,
    int SuccessCount,
    int FailedCount,
    int SkippedCount,
    double SuccessRate           // percentage of Completed out of all processed (non-Pending/Processing)
);
