using System.Text;
using System.Text.Json;
using KromicFlow.Application.Abstractions;
using KromicFlow.Application.Features.Billing.ProcessRazorpayWebhook;
using KromicFlow.Application.Options;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace KromicFlow.Api.Controllers;

/// <summary>
/// Receives Razorpay webhook events.
/// Endpoint: POST /api/v1/webhooks/razorpay
///
/// Configure in Razorpay Dashboard → Webhooks:
///   URL: https://flowapi.kromic.in/api/v1/webhooks/razorpay
///   Secret: must match Razorpay__WebhookSecret env var
///   Events: subscription.activated, subscription.charged, subscription.halted,
///            subscription.cancelled, subscription.completed, subscription.expired,
///            payment.failed
/// </summary>
[Route("api/v1/webhooks/razorpay")]
public sealed class RazorpayWebhooksController(
    IMediator mediator,
    IRazorpayClient razorpayClient,
    IOptions<RazorpayOptions> options,
    ILogger<RazorpayWebhooksController> logger) : ControllerBase
{
    [HttpPost]
    public async Task<IActionResult> Receive(CancellationToken cancellationToken)
    {
        if (!options.Value.Enabled)
        {
            logger.LogDebug("Razorpay webhook received but Razorpay is disabled");
            return Ok(); // always return 200 to prevent Razorpay retries
        }

        // Read raw body — MUST be raw, not parsed, for signature verification
        using var reader = new StreamReader(Request.Body, Encoding.UTF8);
        var rawBody = await reader.ReadToEndAsync(cancellationToken);

        var signature = Request.Headers["X-Razorpay-Signature"].ToString();

        logger.LogInformation("Razorpay webhook received — event signature present: {HasSig}", !string.IsNullOrEmpty(signature));

        // Verify signature — reject if invalid
        if (string.IsNullOrEmpty(signature) || !razorpayClient.VerifyWebhookSignature(rawBody, signature))
        {
            logger.LogWarning("Razorpay webhook signature verification FAILED — rejecting");
            return Unauthorized();
        }

        logger.LogInformation("Razorpay webhook signature verified");

        // Parse event type and subscription data
        ProcessRazorpayWebhookCommand? command;
        try
        {
            command = ParseWebhook(rawBody);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to parse Razorpay webhook payload");
            return BadRequest("Invalid payload");
        }

        if (command is null)
        {
            logger.LogInformation("Razorpay webhook event not handled, returning 200");
            return Ok();
        }

        var result = await mediator.Send(command, cancellationToken);

        if (!result.Succeeded)
            logger.LogWarning("Razorpay webhook processing returned failure: {Error}", result.Error);

        // Always return 200 — Razorpay will retry on non-200
        return Ok();
    }

    private static ProcessRazorpayWebhookCommand? ParseWebhook(string rawBody)
    {
        using var doc = JsonDocument.Parse(rawBody);
        var root = doc.RootElement;

        if (!root.TryGetProperty("event", out var eventEl))
            return null;

        var eventType = eventEl.GetString() ?? string.Empty;

        // Only handle subscription and payment events
        if (!eventType.StartsWith("subscription.") && eventType != "payment.failed")
            return null;

        // Navigate to subscription entity
        if (!root.TryGetProperty("payload", out var payload))
            return null;

        string? subscriptionId = null;
        string? status = null;
        string? customerId = null;
        string? paymentId = null;
        long? currentStart = null;
        long? currentEnd = null;
        int? paidCount = null;

        if (payload.TryGetProperty("subscription", out var subWrapper) &&
            subWrapper.TryGetProperty("entity", out var sub))
        {
            subscriptionId = sub.TryGetProperty("id", out var sid) ? sid.GetString() : null;
            status         = sub.TryGetProperty("status", out var st) ? st.GetString() : null;
            customerId     = sub.TryGetProperty("customer_id", out var cid) && cid.ValueKind != JsonValueKind.Null ? cid.GetString() : null;
            paidCount      = sub.TryGetProperty("paid_count", out var pc) ? pc.GetInt32() : null;

            if (sub.TryGetProperty("current_start", out var cs) && cs.ValueKind != JsonValueKind.Null)
                currentStart = cs.GetInt64();
            if (sub.TryGetProperty("current_end", out var ce) && ce.ValueKind != JsonValueKind.Null)
                currentEnd = ce.GetInt64();
        }

        if (payload.TryGetProperty("payment", out var payWrapper) &&
            payWrapper.TryGetProperty("entity", out var pay))
        {
            paymentId = pay.TryGetProperty("id", out var pid) ? pid.GetString() : null;

            // For payment.failed, try to get subscription_id from payment description
            if (subscriptionId is null && pay.TryGetProperty("subscription_id", out var psid))
                subscriptionId = psid.GetString();
        }

        if (string.IsNullOrEmpty(subscriptionId))
            return null;

        return new ProcessRazorpayWebhookCommand(
            EventType: eventType,
            SubscriptionId: subscriptionId,
            PaymentId: paymentId,
            CustomerId: customerId,
            Status: status,
            CurrentStart: currentStart,
            CurrentEnd: currentEnd,
            PaidCount: paidCount,
            RawPayloadJson: rawBody
        );
    }
}
