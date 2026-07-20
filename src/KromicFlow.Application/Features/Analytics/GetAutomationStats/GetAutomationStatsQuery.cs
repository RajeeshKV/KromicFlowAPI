using MediatR;

namespace KromicFlow.Application.Features.Analytics.GetAutomationStats;

public sealed record GetAutomationStatsQuery(
    Guid UserId,
    Guid InstagramAccountId,
    DateTime? From = null,
    DateTime? To = null
) : IRequest<List<AutomationStatsDto>>;

public sealed record AutomationStatsDto(
    Guid AutomationId,
    string AutomationName,
    bool Enabled,
    int TotalExecutions,
    int SuccessCount,
    int FailedCount,
    int PublicRepliesSent,
    int PrivateRepliesSent,
    double SuccessRate,
    DateTime? LastFiredUtc
);
