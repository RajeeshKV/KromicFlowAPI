using KromicFlow.Application.Abstractions;
using KromicFlow.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace KromicFlow.Application.Features.Analytics.GetTimeSeries;

internal sealed class GetTimeSeriesQueryHandler(IKromicFlowDbContext db)
    : IRequestHandler<GetTimeSeriesQuery, List<TimeSeriesDataPointDto>>
{
    public async Task<List<TimeSeriesDataPointDto>> Handle(GetTimeSeriesQuery request, CancellationToken cancellationToken)
    {
        var account = await db.InstagramAccounts
            .FirstOrDefaultAsync(x => x.Id == request.InstagramAccountId && x.UserId == request.UserId, cancellationToken);

        if (account is null) return [];

        // Clamp range to max 90 days to prevent runaway queries
        var from = request.From.Date;
        var to   = request.To.Date > from.AddDays(90) ? from.AddDays(90) : request.To.Date;
        var toExclusive = to.AddDays(1);

        var eventsQuery = db.WebhookEvents
            .Where(x => x.InstagramAccountId == request.InstagramAccountId
                     && x.ReceivedUtc >= from
                     && x.ReceivedUtc < toExclusive
                     && (x.Status == WebhookStatus.Completed || x.Status == WebhookStatus.Failed));

        if (request.AutomationId.HasValue)
            eventsQuery = eventsQuery.Where(x => x.AutomationId == request.AutomationId.Value);

        // EF Core translates DateOnly via Date property — group by UTC day
        var rawData = await eventsQuery
            .GroupBy(x => x.ReceivedUtc.Date)
            .Select(g => new
            {
                Date               = g.Key,
                TotalExecutions    = g.Count(),
                SuccessCount       = g.Count(x => x.Status == WebhookStatus.Completed),
                FailedCount        = g.Count(x => x.Status == WebhookStatus.Failed),
                PublicRepliesSent  = g.Count(x => x.PublicReplySentUtc.HasValue),
                PrivateRepliesSent = g.Count(x => x.PrivateReplySentUtc.HasValue)
            })
            .OrderBy(x => x.Date)
            .ToListAsync(cancellationToken);

        var dataMap = rawData.ToDictionary(x => x.Date);

        // Build a complete series with zero-filled gaps for every day in range
        var result = new List<TimeSeriesDataPointDto>();
        for (var day = from; day <= to; day = day.AddDays(1))
        {
            var d = dataMap.GetValueOrDefault(day);
            result.Add(new TimeSeriesDataPointDto(
                Date:               day,
                TotalExecutions:    d?.TotalExecutions ?? 0,
                SuccessCount:       d?.SuccessCount ?? 0,
                FailedCount:        d?.FailedCount ?? 0,
                PublicRepliesSent:  d?.PublicRepliesSent ?? 0,
                PrivateRepliesSent: d?.PrivateRepliesSent ?? 0
            ));
        }

        return result;
    }
}
