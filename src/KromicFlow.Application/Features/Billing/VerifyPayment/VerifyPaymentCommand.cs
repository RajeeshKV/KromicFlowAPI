using KromicFlow.Application.Common;
using MediatR;

namespace KromicFlow.Application.Features.Billing.VerifyPayment;

public sealed record VerifyPaymentCommand(
    Guid UserId,
    string RazorpayPaymentId,
    string RazorpaySubscriptionId,
    string RazorpaySignature
) : IRequest<Result<VerifyPaymentResponseDto>>;

public sealed record VerifyPaymentResponseDto(
    string SubscriptionId,
    string Status,
    string PlanCode,
    string PlanName
);
