using KromicFlow.Application.Abstractions;
using KromicFlow.Application.Common;
using KromicFlow.Application.Options;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace KromicFlow.Application.Features.Billing.VerifyPayment;

internal sealed class VerifyPaymentCommandHandler(
    IKromicFlowDbContext db,
    IRazorpayClient razorpay,
    IAuditWriter auditWriter,
    IOptions<RazorpayOptions> razorpayOptions,
    ILogger<VerifyPaymentCommandHandler> logger) : IRequestHandler<VerifyPaymentCommand, Result<VerifyPaymentResponseDto>>
{
    public async Task<Result<VerifyPaymentResponseDto>> Handle(VerifyPaymentCommand request, CancellationToken cancellationToken)
    {
        if (!razorpayOptions.Value.Enabled)
            return Result<VerifyPaymentResponseDto>.Failure("Payment processing is not enabled.");

        logger.LogInformation("Verifying payment {PaymentId} for subscription {SubId}",
            request.RazorpayPaymentId, request.RazorpaySubscriptionId);

        // CRITICAL: verify signature server-side before trusting anything
        var signatureValid = razorpay.VerifyPaymentSignature(
            request.RazorpayPaymentId,
            request.RazorpaySubscriptionId,
            request.RazorpaySignature);

        if (!signatureValid)
        {
            logger.LogWarning("Payment signature INVALID for user {UserId}, paymentId {PaymentId}",
                request.UserId, request.RazorpayPaymentId);
            await auditWriter.WriteAsync("PaymentSignatureInvalid", "UserSubscription",
                request.RazorpaySubscriptionId, request.UserId, null,
                $"{{\"paymentId\":\"{request.RazorpayPaymentId}\"}}",
                cancellationToken);
            return Result<VerifyPaymentResponseDto>.Failure("Payment signature verification failed.");
        }

        var subscription = await db.UserSubscriptions
            .Include(x => x.Plan)
            .FirstOrDefaultAsync(x =>
                x.RazorpaySubscriptionId == request.RazorpaySubscriptionId &&
                x.UserId == request.UserId,
                cancellationToken);

        if (subscription is null)
            return Result<VerifyPaymentResponseDto>.Failure("Subscription not found.");

        // Fetch latest state from Razorpay to confirm it's authenticated/active
        var rzpSub = await razorpay.FetchSubscriptionAsync(request.RazorpaySubscriptionId, cancellationToken);

        subscription.RazorpayPaymentId = request.RazorpayPaymentId;
        subscription.RazorpayCustomerId = rzpSub.CustomerId;
        subscription.Status = rzpSub.Status;
        subscription.PaidCount = rzpSub.PaidCount;
        subscription.TotalCount = rzpSub.TotalCount;
        subscription.RazorpaySnapshotJson = rzpSub.RawJson;

        if (rzpSub.CurrentStart.HasValue)
            subscription.CurrentPeriodStart = DateTimeOffset.FromUnixTimeSeconds(rzpSub.CurrentStart.Value).UtcDateTime;
        if (rzpSub.CurrentEnd.HasValue)
            subscription.CurrentPeriodEnd = DateTimeOffset.FromUnixTimeSeconds(rzpSub.CurrentEnd.Value).UtcDateTime;

        // If subscription is authenticated/active — upgrade the user's plan
        if (rzpSub.Status is "authenticated" or "active")
        {
            subscription.ActivatedAtUtc = DateTime.UtcNow;
            var user = await db.Users.FirstOrDefaultAsync(x => x.Id == request.UserId, cancellationToken);
            if (user is not null)
            {
                user.PlanId = subscription.PlanId;
                user.UpdatedUtc = DateTime.UtcNow;
            }
        }

        await db.SaveChangesAsync(cancellationToken);

        await auditWriter.WriteAsync("PaymentVerified", "UserSubscription",
            subscription.Id.ToString(), request.UserId, null,
            $"{{\"paymentId\":\"{request.RazorpayPaymentId}\",\"status\":\"{rzpSub.Status}\"}}",
            cancellationToken);

        logger.LogInformation("Payment {PaymentId} verified — subscription {SubId} status: {Status}",
            request.RazorpayPaymentId, request.RazorpaySubscriptionId, rzpSub.Status);

        return Result<VerifyPaymentResponseDto>.Success(new VerifyPaymentResponseDto(
            SubscriptionId: subscription.RazorpaySubscriptionId,
            Status: subscription.Status,
            PlanCode: subscription.Plan.Code,
            PlanName: subscription.Plan.Name
        ));
    }
}
