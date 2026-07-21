using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using KromicFlow.Application.Abstractions;
using KromicFlow.Application.Options;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace KromicFlow.Infrastructure.External;

public sealed class RazorpayClient(
    HttpClient httpClient,
    IOptions<RazorpayOptions> options,
    ILogger<RazorpayClient> logger) : IRazorpayClient
{
    private readonly JsonSerializerOptions _json = new()
    {
        PropertyNameCaseInsensitive = true,
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower
    };

    public async Task<RazorpaySubscriptionResult> CreateSubscriptionAsync(
        RazorpayCreateSubscriptionRequest request,
        CancellationToken cancellationToken)
    {
        logger.LogInformation("Creating Razorpay subscription for plan {PlanId}", request.PlanId);

        var body = new
        {
            plan_id = request.PlanId,
            total_count = request.TotalCount == 0 ? 600 : request.TotalCount, // 600 = ~50 years, Razorpay max
            quantity = 1,
            customer_notify = 1,
            notes = new Dictionary<string, string>
            {
                ["user_id"] = request.Notes_UserId ?? string.Empty,
                ["plan_code"] = request.Notes_PlanCode ?? string.Empty
            }
        };

        var json = JsonSerializer.Serialize(body, _json);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        var response = await SendAsync(HttpMethod.Post, "subscriptions", content, cancellationToken);
        var raw = await response.Content.ReadAsStringAsync(cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            logger.LogError("Razorpay CreateSubscription failed: {Status} — {Body}", response.StatusCode, raw);
            throw new RazorpayException($"Razorpay CreateSubscription failed ({response.StatusCode}): {raw}");
        }

        logger.LogInformation("Razorpay subscription created successfully");
        return ParseSubscription(raw);
    }

    public async Task<RazorpaySubscriptionResult> FetchSubscriptionAsync(
        string subscriptionId,
        CancellationToken cancellationToken)
    {
        logger.LogInformation("Fetching Razorpay subscription {SubscriptionId}", subscriptionId);

        var response = await SendAsync(HttpMethod.Get, $"subscriptions/{subscriptionId}", null, cancellationToken);
        var raw = await response.Content.ReadAsStringAsync(cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            logger.LogError("Razorpay FetchSubscription failed: {Status} — {Body}", response.StatusCode, raw);
            throw new RazorpayException($"Razorpay FetchSubscription failed ({response.StatusCode}): {raw}");
        }

        return ParseSubscription(raw);
    }

    public async Task<RazorpaySubscriptionResult> CancelSubscriptionAsync(
        string subscriptionId,
        bool cancelAtCycleEnd,
        CancellationToken cancellationToken)
    {
        logger.LogInformation("Cancelling Razorpay subscription {SubscriptionId} (atCycleEnd={AtCycleEnd})",
            subscriptionId, cancelAtCycleEnd);

        var body = new { cancel_at_cycle_end = cancelAtCycleEnd ? 1 : 0 };
        var content = new StringContent(JsonSerializer.Serialize(body, _json), Encoding.UTF8, "application/json");

        var response = await SendAsync(HttpMethod.Post, $"subscriptions/{subscriptionId}/cancel", content, cancellationToken);
        var raw = await response.Content.ReadAsStringAsync(cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            logger.LogError("Razorpay CancelSubscription failed: {Status} — {Body}", response.StatusCode, raw);
            throw new RazorpayException($"Razorpay CancelSubscription failed ({response.StatusCode}): {raw}");
        }

        logger.LogInformation("Razorpay subscription {SubscriptionId} cancelled", subscriptionId);
        return ParseSubscription(raw);
    }

    public bool VerifyPaymentSignature(string paymentId, string subscriptionId, string signature)
    {
        // Razorpay payment signature: HMAC_SHA256(paymentId + "|" + subscriptionId, keySecret)
        var message = $"{paymentId}|{subscriptionId}";
        var expected = ComputeHmacSha256(message, options.Value.KeySecret);
        var isValid = CryptographicOperations.FixedTimeEquals(
            Encoding.UTF8.GetBytes(expected),
            Encoding.UTF8.GetBytes(signature));

        if (!isValid)
            logger.LogWarning("Razorpay payment signature verification FAILED for payment {PaymentId}", paymentId);

        return isValid;
    }

    public bool VerifyWebhookSignature(string rawBody, string signature)
    {
        // Razorpay webhook signature: HMAC_SHA256(rawBody, webhookSecret)
        var expected = ComputeHmacSha256(rawBody, options.Value.WebhookSecret);
        var isValid = CryptographicOperations.FixedTimeEquals(
            Encoding.UTF8.GetBytes(expected),
            Encoding.UTF8.GetBytes(signature));

        if (!isValid)
            logger.LogWarning("Razorpay webhook signature verification FAILED");

        return isValid;
    }

    // ── Helpers ──────────────────────────────────────────────────────────────

    private async Task<HttpResponseMessage> SendAsync(
        HttpMethod method, string path, HttpContent? content, CancellationToken cancellationToken)
    {
        var request = new HttpRequestMessage(method, $"{options.Value.ApiBaseUrl}/{path}");
        request.Headers.Authorization = new AuthenticationHeaderValue(
            "Basic",
            Convert.ToBase64String(Encoding.UTF8.GetBytes($"{options.Value.KeyId}:{options.Value.KeySecret}")));

        if (content is not null)
            request.Content = content;

        return await httpClient.SendAsync(request, cancellationToken);
    }

    private RazorpaySubscriptionResult ParseSubscription(string raw)
    {
        using var doc = JsonDocument.Parse(raw);
        var root = doc.RootElement;

        return new RazorpaySubscriptionResult(
            SubscriptionId: root.TryGetProperty("id", out var id) ? id.GetString() ?? string.Empty : string.Empty,
            PlanId:         root.TryGetProperty("plan_id", out var pid) ? pid.GetString() ?? string.Empty : string.Empty,
            Status:         root.TryGetProperty("status", out var st) ? st.GetString() ?? "created" : "created",
            ShortUrl:       root.TryGetProperty("short_url", out var url) ? url.GetString() : null,
            TotalCount:     root.TryGetProperty("total_count", out var tc) ? tc.GetInt32() : 0,
            PaidCount:      root.TryGetProperty("paid_count", out var pc) ? pc.GetInt32() : 0,
            CurrentStart:   root.TryGetProperty("current_start", out var cs) && cs.ValueKind != JsonValueKind.Null ? cs.GetInt64() : null,
            CurrentEnd:     root.TryGetProperty("current_end", out var ce) && ce.ValueKind != JsonValueKind.Null ? ce.GetInt64() : null,
            CustomerId:     root.TryGetProperty("customer_id", out var cid) && cid.ValueKind != JsonValueKind.Null ? cid.GetString() : null,
            RawJson:        raw
        );
    }

    private static string ComputeHmacSha256(string message, string secret)
    {
        using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(secret));
        var hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(message));
        return Convert.ToHexString(hash).ToLowerInvariant();
    }
}

public sealed class RazorpayException(string message) : Exception(message);
