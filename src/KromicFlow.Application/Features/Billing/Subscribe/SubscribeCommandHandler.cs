using KromicFlow.Application.Abstractions;
using KromicFlow.Application.Common;
using KromicFlow.Application.Options;
using KromicFlow.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace KromicFlow.Application.Features.Billing.Subscribe;

internal sealed class SubscribeCommandHandler(
    IKromicFlowDbContext db,
    IRazorpayClient razorpay,
    IAuditWriter auditWriter,
    IOptions<RazorpayOptions> razorpayOptions,
    ILogger<SubscribeCommandHandler> logger) : IRequestHandler<SubscribeCommand, Result<SubscribeResponseDto>>
{
    public async Task<Result<SubscribeResponseDto>> Handle(SubscribeCommand request, CancellationToken cancellationToken)
    {
        if (!razorpayOptions.Value.Enabled)
            return Result<SubscribeResponseDto>.Failure("Payment processing is not enabled.");

        var user = await db.Users.Include(x => x.Plan)
            .FirstOrDefaultAsync(x => x.Id == request.UserId, cancellationToken);
        if (user is null) return Result<SubscribeResponseDto>.Failure("User not found.");

        var plan = await db.Plans
            .FirstOrDefaultAsync(x => x.Code == request.PlanCode && x.IsActive, cancellationToken);
        if (plan is null) return Result<SubscribeResponseDto>.Failure("Plan not found.");
        if (plan.PriceInrPaise == 0) return Result<SubscribeResponseDto>.Failure("Free plan does not require payment.");
        if (string.IsNullOrEmpty(plan.RazorpayPlanId))
            return Result<SubscribeResponseDto>.Failure("This plan is not configured for payments. Contact support.");

        // Block if user already has an active/authenticated subscription for this plan
        var existingActive = await db.UserSubscriptions
            .AnyAsync(x => x.UserId == request.UserId &&
                           (x.Status == "active" || x.Status == "authenticated" || x.Status == "created"),
                cancellationToken);
        if (existingActive)
            return Result<SubscribeResponseDto>.Failure("You already have an active subscription. Cancel it before subscribing to a new plan.");

        logger.LogInformation("Creating Razorpay subscription for user {UserId} on plan {PlanCode}", request.UserId, request.PlanCode);

        var razorpayResult = await razorpay.CreateSubscriptionAsync(
            new RazorpayCreateSubscriptionRequest(
                PlanId: plan.RazorpayPlanId,
                TotalCount: razorpayOptions.Value.DefaultTotalCount,
                Notes_UserId: request.UserId.ToString(),
                Notes_PlanCode: plan.Code
            ), cancellationToken);

        // Persist the subscription record immediately so we can track it
        var subscription = new UserSubscription
        {
            UserId = request.UserId,
            PlanId = plan.Id,
            RazorpaySubscriptionId = razorpayResult.SubscriptionId,
            RazorpayPlanId = razorpayResult.PlanId,
            Status = razorpayResult.Status,
            TotalCount = razorpayResult.TotalCount,
            RazorpaySnapshotJson = razorpayResult.RawJson
        };
        db.UserSubscriptions.Add(subscription);
        await db.SaveChangesAsync(cancellationToken);

        await auditWriter.WriteAsync("SubscriptionCreated", nameof(UserSubscription),
            subscription.Id.ToString(), request.UserId, null,
            $"{{\"razorpaySubId\":\"{razorpayResult.SubscriptionId}\",\"plan\":\"{plan.Code}\"}}",
            cancellationToken);

        logger.LogInformation("Razorpay subscription {SubId} created for user {UserId}",
            razorpayResult.SubscriptionId, request.UserId);

        return Result<SubscribeResponseDto>.Success(new SubscribeResponseDto(
            RazorpayKeyId: razorpayOptions.Value.KeyId,
            RazorpaySubscriptionId: razorpayResult.SubscriptionId,
            PlanName: plan.Name,
            AmountInrPaise: plan.PriceInrPaise,
            UserEmail: user.Email ?? string.Empty,
            UserFullName: user.FullName
        ));
    }
}
