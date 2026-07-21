using KromicFlow.Application.Common;
using MediatR;

namespace KromicFlow.Application.Features.Billing.ProcessRazorpayWebhook;

public sealed record ProcessRazorpayWebhookCommand(
    string EventType,       // e.g. "subscription.activated"
    string SubscriptionId,  // sub_xxxx
    string? PaymentId,      // pay_xxxx (present on charged events)
    string? CustomerId,     // cust_xxxx
    string? Status,         // latest subscription status from payload
    long? CurrentStart,
    long? CurrentEnd,
    int? PaidCount,
    string RawPayloadJson   // stored verbatim for audit
) : IRequest<Result>;
