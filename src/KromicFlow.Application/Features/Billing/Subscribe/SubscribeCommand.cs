using KromicFlow.Application.Common;
using MediatR;

namespace KromicFlow.Application.Features.Billing.Subscribe;

public sealed record SubscribeCommand(Guid UserId, string PlanCode) : IRequest<Result<SubscribeResponseDto>>;

public sealed record SubscribeResponseDto(
    string RazorpayKeyId,           // pass to Razorpay Checkout as "key"
    string RazorpaySubscriptionId,  // pass to Razorpay Checkout as "subscription_id"
    string PlanName,
    int AmountInrPaise,
    string UserEmail,
    string UserFullName
);
