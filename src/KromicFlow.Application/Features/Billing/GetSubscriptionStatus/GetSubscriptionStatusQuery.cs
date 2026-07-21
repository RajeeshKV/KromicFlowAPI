using MediatR;

namespace KromicFlow.Application.Features.Billing.GetSubscriptionStatus;

public sealed record GetSubscriptionStatusQuery(Guid UserId) : IRequest<SubscriptionStatusDto>;

public sealed record SubscriptionStatusDto(
    bool HasActiveSubscription,
    string? SubscriptionId,            // Razorpay sub_xxxx
    string? Status,                    // created|authenticated|active|pending|halted|cancelled|completed|expired
    string PlanCode,
    string PlanName,
    int PriceInrPaise,
    int PriceInrRupees,
    string BillingPeriod,
    DateTime? CurrentPeriodStart,
    DateTime? CurrentPeriodEnd,
    DateTime? CancelledAtUtc,
    DateTime? ActivatedAtUtc,
    int PaidCount,
    int TotalCount,
    bool WillCancelAtCycleEnd          // true when status=cancelled but period hasn't ended yet
);
