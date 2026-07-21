namespace KromicFlow.Application.Options;

/// <summary>
/// Razorpay configuration bound from Razorpay__ environment variables.
/// Set Razorpay__Enabled=false to completely bypass payment processing (useful in dev/staging).
/// </summary>
public sealed class RazorpayOptions
{
    /// <summary>
    /// Master switch. When false, all billing endpoints return a 503 and no Razorpay calls are made.
    /// Set to true only when KeyId and KeySecret are configured.
    /// </summary>
    public bool Enabled { get; set; } = false;

    /// <summary>Razorpay Key ID — starts with rzp_test_ in test mode, rzp_live_ in production.</summary>
    public string KeyId { get; set; } = string.Empty;

    /// <summary>Razorpay Key Secret — never expose to the frontend.</summary>
    public string KeySecret { get; set; } = string.Empty;

    /// <summary>
    /// Webhook secret configured in Razorpay Dashboard → Webhooks → Secret.
    /// Used to verify X-Razorpay-Signature on incoming webhook events.
    /// </summary>
    public string WebhookSecret { get; set; } = string.Empty;

    /// <summary>Base URL for Razorpay API. Do not change unless using a proxy.</summary>
    public string ApiBaseUrl { get; set; } = "https://api.razorpay.com/v1";

    /// <summary>
    /// Number of billing cycles for new subscriptions. 0 = unlimited (until cancelled).
    /// For monthly plans: 12 = 1 year, 0 = recurring indefinitely.
    /// </summary>
    public int DefaultTotalCount { get; set; } = 0;
}
