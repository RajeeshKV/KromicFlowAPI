using KromicFlow.Application.Abstractions;
using KromicFlow.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace KromicFlow.Application.Features.Analytics.GetAutomationStats;

internal sealed class GetAutomationStatsQueryHandler(IKromicFlowDbContext db)
    : IRequestHandler<GetAutomationStatsQuery, List<AutomationStatsDto>>
{
    public async Task<List<AutomationStatsDto>> Handle(GetAutomationStatsQuery request, CancellationToken cancellationToken)
    {
        // Verify the account belongs to this user
        var account = await db.InstagramAccounts
            .FirstOrDefaultAsync(x => x.Id == request.InstagramAccountId && x.UserId == request.UserId, cancellationToken);

        if (account is null)
            return [];

        // Load all automations for this account
        var automations = await db.Automations
            .Where(x => x.InstagramAccountId == request.InstagramAccountId)
            .OrderBy(x => x.Priority)
            .ThenBy(x => x.CreatedUtc)
            .Select(x => new { x.Id, x.Name, x.Enabled })
            .ToListAsync(cancellationToken);

        if (automations.Count == 0)
            return [];

        var automationIds = automations.Select(x => x.Id).ToList();

        // Aggregate stats per automation in a single query
        var stats = await db.WebhookEvents
            .Where(x => x.AutomationId.HasValue && automationIds.Contains(x.AutomationId!.Value))
            .GroupBy(x => x.AutomationId!.Value)
            .Select(g => new
            {
                AutomationId = g.Key,
                TotalExecutions = g.Count(x => x.Status == WebhookStatus.Completed || x.Status == WebhookStatus.Failed),
                SuccessCount = g.Count(x => x.Status == WebhookStatus.Completed),
                FailedCount = g.Count(x => x.Status == WebhookStatus.Failed),
                PublicRepliesSent = g.Count(x => x.PublicReplySentUtc.HasValue),
                PrivateRepliesSent = g.Count(x => x.PrivateReplySentUtc.HasValue),
                LastFiredUtc = g.Max(x => x.ProcessedUtc)
            })
            .ToListAsync(cancellationToken);

        var statsMap = stats.ToDictionary(x => x.AutomationId);

        return automations.Select(a =>
        {
            var s = statsMap.GetValueOrDefault(a.Id);
            var total = s?.TotalExecutions ?? 0;
            var success = s?.SuccessCount ?? 0;
            return new AutomationStatsDto(
                AutomationId: a.Id,
                AutomationName: a.Name,
                Enabled: a.Enabled,
                TotalExecutions: total,
                SuccessCount: success,
                FailedCount: s?.FailedCount ?? 0,
                PublicRepliesSent: s?.PublicRepliesSent ?? 0,
                PrivateRepliesSent: s?.PrivateRepliesSent ?? 0,
                SuccessRate: total > 0 ? Math.Round((double)success / total * 100, 1) : 0.0,
                LastFiredUtc: s?.LastFiredUtc
            );
        }).ToList();
    }
}
