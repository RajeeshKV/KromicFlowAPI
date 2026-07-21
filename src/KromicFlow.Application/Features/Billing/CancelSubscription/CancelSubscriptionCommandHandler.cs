using KromicFlow.Application.Abstractions;
using KromicFlow.Application.Common;
using KromicFlow.Application.Options;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace KromicFlow.Application.Features.Billing.CancelSubscription;

internal sealed class CancelSubscriptionCommandHandler(
    IKromicFlowDbContext db,
    IRazorpayClient razorpay,
    IAuditWriter auditWriter,
    IOptions<RazorpayOptions> razorpayOptions,
    ILogger<CancelSubscriptionCommandHandler> logger) : IRequestHandler<CancelSubscriptionCommand, Result>
{
    public async Task<Result> Handle(CancelSubscriptionCommand request, CancellationToken cancellationToken)
    {
        if (!razorpayOptions.Value.Enabled)
            return Result.Failure("Payment processing is not enabled.");

        var subscription = await db.UserSubscriptions
            .FirstOrDefaultAsync(x =>
                x.UserId == request.UserId &&
                (x.Status == "active" || x.Status == "authenticated"),
                cancellationToken);

        if (subscription is null)
            return Result.Failure("No active subscription found.");

        logger.LogInformation("Cancelling subscription {SubId} for user {UserId} (atCycleEnd={AtCycleEnd})",
            subscription.RazorpaySubscriptionId, request.UserId, request.CancelAtCycleEnd);

        var result = await razorpay.CancelSubscriptionAsync(
            subscription.RazorpaySubscriptionId,
            request.CancelAtCycleEnd,
            cancellationToken);

        subscription.Status = result.Status;
        subscription.CancelledAtUtc = DateTime.UtcNow;
        subscription.RazorpaySnapshotJson = result.RawJson;
        subscription.UpdatedUtc = DateTime.UtcNow;

        // If cancelling immediately, downgrade user to free plan right now
        if (!request.CancelAtCycleEnd)
        {
            var freePlan = await db.Plans.FirstOrDefaultAsync(x => x.Code == "free", cancellationToken);
            if (freePlan is not null)
            {
                var user = await db.Users.FirstOrDefaultAsync(x => x.Id == request.UserId, cancellationToken);
                if (user is not null)
                {
                    user.PlanId = freePlan.Id;
                    user.UpdatedUtc = DateTime.UtcNow;
                    logger.LogInformation("User {UserId} downgraded to free plan (immediate cancel)", request.UserId);
                }
            }
        }
        // If cancelAtCycleEnd=true, downgrade happens via the subscription.completed/cancelled webhook

        await db.SaveChangesAsync(cancellationToken);

        await auditWriter.WriteAsync("SubscriptionCancelled", "UserSubscription",
            subscription.Id.ToString(), request.UserId, null,
            $"{{\"subId\":\"{subscription.RazorpaySubscriptionId}\",\"atCycleEnd\":{request.CancelAtCycleEnd.ToString().ToLower()}}}",
            cancellationToken);

        logger.LogInformation("Subscription {SubId} cancelled (status: {Status})",
            subscription.RazorpaySubscriptionId, result.Status);

        return Result.Success();
    }
}
