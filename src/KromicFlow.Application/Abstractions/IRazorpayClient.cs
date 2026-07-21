namespace KromicFlow.Application.Abstractions;

public sealed record RazorpaySubscriptionResult(
    string SubscriptionId,
    string PlanId,
    string Status,
    string? ShortUrl,
    int TotalCount,
    int PaidCount,
    long? CurrentStart,
    long? CurrentEnd,
    string? CustomerId,
    string RawJson
);

public sealed record RazorpayCreateSubscriptionRequest(
    string PlanId,
    int TotalCount,          // 0 = unlimited cycles
    string? Notes_UserId,
    string? Notes_PlanCode
);

public interface IRazorpayClient
{
    /// <summary>
    /// Creates a Razorpay subscription for a plan.
    /// Returns the subscription object including the subscription_id needed for Checkout.
    /// </summary>
    Task<RazorpaySubscriptionResult> CreateSubscriptionAsync(
        RazorpayCreateSubscriptionRequest request,
        CancellationToken cancellationToken);

    /// <summary>
    /// Fetches the current state of a subscription from Razorpay.
    /// Used to sync status after webhook events.
    /// </summary>
    Task<RazorpaySubscriptionResult> FetchSubscriptionAsync(
        string subscriptionId,
        CancellationToken cancellationToken);

    /// <summary>
    /// Cancels a subscription at the end of the current billing cycle.
    /// Set cancelAtCycleEnd=false to cancel immediately.
    /// </summary>
    Task<RazorpaySubscriptionResult> CancelSubscriptionAsync(
        string subscriptionId,
        bool cancelAtCycleEnd,
        CancellationToken cancellationToken);

    /// <summary>
    /// Verifies the HMAC-SHA256 payment signature returned by Razorpay Checkout.
    /// signature = HMAC_SHA256(razorpay_payment_id + "|" + razorpay_subscription_id, key_secret)
    /// </summary>
    bool VerifyPaymentSignature(string paymentId, string subscriptionId, string signature);

    /// <summary>
    /// Verifies the HMAC-SHA256 signature on an incoming Razorpay webhook event.
    /// signature = HMAC_SHA256(raw_body, webhook_secret)
    /// </summary>
    bool VerifyWebhookSignature(string rawBody, string signature);
}
