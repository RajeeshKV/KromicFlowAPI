using MediatR;

namespace KromicFlow.Application.Features.Analytics.GetTimeSeries;

public sealed record GetTimeSeriesQuery(
    Guid UserId,
    Guid InstagramAccountId,
    DateTime From,
    DateTime To,
    Guid? AutomationId = null   // optional: scope to one automation
) : IRequest<List<TimeSeriesDataPointDto>>;

public sealed record TimeSeriesDataPointDto(
    DateTime Date,              // UTC day (midnight)
    int TotalExecutions,
    int SuccessCount,
    int FailedCount,
    int PublicRepliesSent,
    int PrivateRepliesSent
);
