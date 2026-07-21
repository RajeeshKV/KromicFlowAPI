using MediatR;

namespace KromicFlow.Application.Features.Billing.GetPlans;

public sealed record GetPlansQuery : IRequest<List<PlanDto>>;

public sealed record PlanDto(
    Guid Id,
    string Code,
    string Name,
    int PriceInrPaise,
    int PriceInrRupees,         // convenience: PriceInrPaise / 100
    string BillingPeriod,
    int MaxInstagramAccounts,
    int MaxAutomations,
    int MonthlyAutomationRuns,
    bool HasRazorpayPlan,       // false = free plan or Razorpay not configured for this plan
    bool IsFree
);
