using KromicFlow.Application.Abstractions;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace KromicFlow.Application.Features.Billing.GetSubscriptionStatus;

internal sealed class GetSubscriptionStatusQueryHandler(IKromicFlowDbContext db)
    : IRequestHandler<GetSubscriptionStatusQuery, SubscriptionStatusDto>
{
    public async Task<SubscriptionStatusDto> Handle(GetSubscriptionStatusQuery request, CancellationToken cancellationToken)
    {
        // Get most recent non-expired subscription
        var subscription = await db.UserSubscriptions
            .Include(x => x.Plan)
            .Where(x => x.UserId == request.UserId)
            .OrderByDescending(x => x.CreatedUtc)
            .FirstOrDefaultAsync(cancellationToken);

        // Always include current plan from User for the base plan info
        var user = await db.Users
            .Include(x => x.Plan)
            .FirstOrDefaultAsync(x => x.Id == request.UserId, cancellationToken);

        var plan = subscription?.Plan ?? user?.Plan;

        if (plan is null)
        {
            // Fallback — should never happen in practice
            return new SubscriptionStatusDto(
                HasActiveSubscription: false, SubscriptionId: null, Status: null,
                PlanCode: "free", PlanName: "Free", PriceInrPaise: 0, PriceInrRupees: 0,
                BillingPeriod: "monthly", CurrentPeriodStart: null, CurrentPeriodEnd: null,
                CancelledAtUtc: null, ActivatedAtUtc: null, PaidCount: 0, TotalCount: 0,
                WillCancelAtCycleEnd: false);
        }

        var isActive = subscription is not null &&
                       subscription.Status is "active" or "authenticated";

        // "Will cancel at cycle end" = subscription cancelled but period hasn't finished yet
        var willCancel = subscription is not null &&
                         subscription.Status == "cancelled" &&
                         subscription.CurrentPeriodEnd.HasValue &&
                         subscription.CurrentPeriodEnd.Value > DateTime.UtcNow;

        return new SubscriptionStatusDto(
            HasActiveSubscription: isActive,
            SubscriptionId: subscription?.RazorpaySubscriptionId,
            Status: subscription?.Status,
            PlanCode: plan.Code,
            PlanName: plan.Name,
            PriceInrPaise: plan.PriceInrPaise,
            PriceInrRupees: plan.PriceInrPaise / 100,
            BillingPeriod: plan.BillingPeriod,
            CurrentPeriodStart: subscription?.CurrentPeriodStart,
            CurrentPeriodEnd: subscription?.CurrentPeriodEnd,
            CancelledAtUtc: subscription?.CancelledAtUtc,
            ActivatedAtUtc: subscription?.ActivatedAtUtc,
            PaidCount: subscription?.PaidCount ?? 0,
            TotalCount: subscription?.TotalCount ?? 0,
            WillCancelAtCycleEnd: willCancel
        );
    }
}
