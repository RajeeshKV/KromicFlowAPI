using KromicFlow.Application.Common;
using MediatR;

namespace KromicFlow.Application.Features.Billing.CancelSubscription;

public sealed record CancelSubscriptionCommand(
    Guid UserId,
    bool CancelAtCycleEnd = true   // default: cancel at end of billing period, not immediately
) : IRequest<Result>;
