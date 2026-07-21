using KromicFlow.Application.Abstractions;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace KromicFlow.Application.Features.Billing.GetPlans;

internal sealed class GetPlansQueryHandler(IKromicFlowDbContext db)
    : IRequestHandler<GetPlansQuery, List<PlanDto>>
{
    public async Task<List<PlanDto>> Handle(GetPlansQuery request, CancellationToken cancellationToken)
    {
        var plans = await db.Plans
            .Where(x => x.IsActive)
            .OrderBy(x => x.PriceInrPaise)
            .ToListAsync(cancellationToken);

        return plans.Select(p => new PlanDto(
            Id: p.Id,
            Code: p.Code,
            Name: p.Name,
            PriceInrPaise: p.PriceInrPaise,
            PriceInrRupees: p.PriceInrPaise / 100,
            BillingPeriod: p.BillingPeriod,
            MaxInstagramAccounts: p.MaxInstagramAccounts,
            MaxAutomations: p.MaxAutomations,
            MonthlyAutomationRuns: p.MonthlyAutomationRuns,
            HasRazorpayPlan: !string.IsNullOrEmpty(p.RazorpayPlanId),
            IsFree: p.PriceInrPaise == 0
        )).ToList();
    }
}
