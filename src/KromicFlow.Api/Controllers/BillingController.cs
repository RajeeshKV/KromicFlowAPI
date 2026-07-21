using KromicFlow.Application.Features.Billing.CancelSubscription;
using KromicFlow.Application.Features.Billing.GetPlans;
using KromicFlow.Application.Features.Billing.GetSubscriptionStatus;
using KromicFlow.Application.Features.Billing.Subscribe;
using KromicFlow.Application.Features.Billing.VerifyPayment;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace KromicFlow.Api.Controllers;

[Authorize]
[Route("api/v1/billing")]
public sealed class BillingController(IMediator mediator) : ApiControllerBase
{
    /// <summary>
    /// Returns all active plans with pricing.
    /// Public — no auth required so unauthenticated users can see the pricing page.
    /// </summary>
    [AllowAnonymous]
    [HttpGet("plans")]
    public async Task<IActionResult> Plans(CancellationToken cancellationToken) =>
        Ok(await mediator.Send(new GetPlansQuery(), cancellationToken));

    /// <summary>
    /// Step 1 of checkout: creates a Razorpay subscription and returns the
    /// subscription_id + key_id needed to open Razorpay Checkout on the frontend.
    /// </summary>
    [HttpPost("subscribe")]
    public async Task<IActionResult> Subscribe([FromBody] SubscribeRequest request, CancellationToken cancellationToken) =>
        FromResult(await mediator.Send(new SubscribeCommand(User.GetSubjectId(), request.PlanCode), cancellationToken));

    /// <summary>
    /// Step 2 of checkout: verifies the Razorpay payment signature server-side.
    /// Call this after the Razorpay Checkout handler fires with payment_id, subscription_id, signature.
    /// On success the user's plan is upgraded immediately.
    /// </summary>
    [HttpPost("verify")]
    public async Task<IActionResult> Verify([FromBody] VerifyPaymentRequest request, CancellationToken cancellationToken) =>
        FromResult(await mediator.Send(
            new VerifyPaymentCommand(
                User.GetSubjectId(),
                request.RazorpayPaymentId,
                request.RazorpaySubscriptionId,
                request.RazorpaySignature),
            cancellationToken));

    /// <summary>
    /// Cancels the user's active subscription.
    /// Default: cancel at end of current billing cycle (user keeps access until period ends).
    /// Pass cancelAtCycleEnd=false to revoke access immediately.
    /// </summary>
    [HttpPost("cancel")]
    public async Task<IActionResult> Cancel([FromBody] CancelRequest request, CancellationToken cancellationToken) =>
        FromResult(await mediator.Send(
            new CancelSubscriptionCommand(User.GetSubjectId(), request.CancelAtCycleEnd),
            cancellationToken));

    /// <summary>
    /// Returns the current subscription status, plan details, and billing period dates.
    /// Use this to render the billing/settings screen.
    /// </summary>
    [HttpGet("status")]
    public async Task<IActionResult> Status(CancellationToken cancellationToken) =>
        Ok(await mediator.Send(new GetSubscriptionStatusQuery(User.GetSubjectId()), cancellationToken));
}

public sealed record SubscribeRequest(string PlanCode);
public sealed record VerifyPaymentRequest(string RazorpayPaymentId, string RazorpaySubscriptionId, string RazorpaySignature);
public sealed record CancelRequest(bool CancelAtCycleEnd = true);
