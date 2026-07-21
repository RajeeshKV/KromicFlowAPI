using KromicFlow.Application.Abstractions;
using KromicFlow.Application.Common;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace KromicFlow.Application.Features.Billing.ProcessRazorpayWebhook;

internal sealed class ProcessRazorpayWebhookCommandHandler(
    IKromicFlowDbContext db,
    IAuditWriter auditWriter,
    ILogger<ProcessRazorpayWebhookCommandHandler> logger)
    : IRequestHandler<ProcessRazorpayWebhookCommand, Result>
{
    public async Task<Result> Handle(ProcessRazorpayWebhookCommand request, CancellationToken cancellationToken)
    {
        logger.LogInformation("Processing Razorpay webhook event {EventType} for subscription {SubId}",
            request.EventType, request.SubscriptionId);

        var subscription = await db.UserSubscriptions
            .Include(x => x.Plan)
            .FirstOrDefaultAsync(x => x.RazorpaySubscriptionId == request.SubscriptionId, cancellationToken);

        if (subscription is null)
        {
            // Webhook arrived before our DB record (race condition) — log and return 200 so Razorpay doesn't retry
            logger.LogWarning("Razorpay webhook {EventType} received for unknown subscription {SubId}",
                request.EventType, request.SubscriptionId);
            return Result.Success();
        }

        // Update subscription fields from webhook payload
        if (!string.IsNullOrEmpty(request.Status))
            subscription.Status = request.Status;
        if (!string.IsNullOrEmpty(request.CustomerId))
            subscription.RazorpayCustomerId = request.CustomerId;
        if (!string.IsNullOrEmpty(request.PaymentId))
            subscription.RazorpayPaymentId = request.PaymentId;
        if (request.PaidCount.HasValue)
            subscription.PaidCount = request.PaidCount.Value;
        if (request.CurrentStart.HasValue)
            subscription.CurrentPeriodStart = DateTimeOffset.FromUnixTimeSeconds(request.CurrentStart.Value).UtcDateTime;
        if (request.CurrentEnd.HasValue)
            subscription.CurrentPeriodEnd = DateTimeOffset.FromUnixTimeSeconds(request.CurrentEnd.Value).UtcDateTime;

        subscription.RazorpaySnapshotJson = request.RawPayloadJson;
        subscription.UpdatedUtc = DateTime.UtcNow;

        var user = await db.Users.Include(x => x.Plan)
            .FirstOrDefaultAsync(x => x.Id == subscription.UserId, cancellationToken);

        switch (request.EventType)
        {
            case "subscription.activated":
            case "subscription.charged":
                // Subscription is active and payment succeeded — ensure user is on the correct plan
                subscription.ActivatedAtUtc ??= DateTime.UtcNow;
                if (user is not null && user.PlanId != subscription.PlanId)
                {
                    user.PlanId = subscription.PlanId;
                    user.UpdatedUtc = DateTime.UtcNow;
                    logger.LogInformation("User {UserId} plan upgraded to {PlanCode} via webhook {Event}",
                        subscription.UserId, subscription.Plan.Code, request.EventType);
                }
                break;

            case "subscription.halted":
                // Payment failed after all retries — keep subscription record but log prominently
                logger.LogWarning("Subscription {SubId} HALTED for user {UserId} — payment failed after retries",
                    request.SubscriptionId, subscription.UserId);
                break;

            case "subscription.cancelled":
            case "subscription.completed":
            case "subscription.expired":
                // Subscription ended — downgrade user to free plan
                subscription.CancelledAtUtc = DateTime.UtcNow;
                var freePlan = await db.Plans.FirstOrDefaultAsync(x => x.Code == "free", cancellationToken);
                if (freePlan is not null && user is not null)
                {
                    user.PlanId = freePlan.Id;
                    user.UpdatedUtc = DateTime.UtcNow;
                    logger.LogInformation("User {UserId} downgraded to free plan — event: {Event}",
                        subscription.UserId, request.EventType);
                }
                break;

            case "payment.failed":
                // Log individual payment failure (subscription may retry automatically)
                logger.LogWarning("Payment failed for subscription {SubId} (attempt for user {UserId})",
                    request.SubscriptionId, subscription.UserId);
                break;

            default:
                logger.LogInformation("Unhandled Razorpay event type {EventType} — recorded but no action taken",
                    request.EventType);
                break;
        }

        await db.SaveChangesAsync(cancellationToken);

        await auditWriter.WriteAsync($"Razorpay:{request.EventType}", "UserSubscription",
            subscription.Id.ToString(), subscription.UserId, null,
            $"{{\"subId\":\"{request.SubscriptionId}\",\"paymentId\":\"{request.PaymentId}\"}}",
            cancellationToken);

        logger.LogInformation("Razorpay webhook {EventType} processed for subscription {SubId}",
            request.EventType, request.SubscriptionId);

        return Result.Success();
    }
}
