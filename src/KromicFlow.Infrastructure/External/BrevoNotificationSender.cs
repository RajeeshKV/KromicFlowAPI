using System.Net.Http.Headers;
using System.Net.Http.Json;
using KromicFlow.Application.Abstractions;
using KromicFlow.Application.Options;
using Microsoft.Extensions.Options;

namespace KromicFlow.Infrastructure.External;

public sealed class BrevoNotificationSender(HttpClient httpClient, IOptions<BrevoOptions> options) : INotificationSender
{
    public async Task<string?> SendEmailAsync(string toEmail, string subject, string body, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(options.Value.ApiKey))
        {
            throw new InvalidOperationException("Brevo API key is not configured. Set Brevo__ApiKey environment variable.");
        }

        if (string.IsNullOrWhiteSpace(options.Value.SenderEmail))
        {
            throw new InvalidOperationException("Brevo sender email is not configured. Set Brevo__SenderEmail environment variable.");
        }

        httpClient.DefaultRequestHeaders.Remove("api-key");
        httpClient.DefaultRequestHeaders.Add("api-key", options.Value.ApiKey);
        httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

        var payload = new
        {
            sender = new { email = options.Value.SenderEmail, name = options.Value.SenderName },
            to = new[] { new { email = toEmail } },
            subject,
            htmlContent = body
        };

        try
        {
            var response = await httpClient.PostAsJsonAsync("/v3/smtp/email", payload, cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync(cancellationToken);
                throw new InvalidOperationException($"Brevo API error ({response.StatusCode}): {errorContent}");
            }
            return response.Headers.TryGetValues("x-message-id", out var values) ? values.FirstOrDefault() : null;
        }
        catch (HttpRequestException ex)
        {
            throw new InvalidOperationException("Failed to send email via Brevo. Check API key configuration.", ex);
        }
    }

    /// <summary>
    /// Send email using Brevo transactional template
    /// </summary>
    public async Task<string?> SendEmailWithTemplateAsync(
        string toEmail,
        int templateId,
        Dictionary<string, string> templateParams,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(options.Value.ApiKey)) return null;

        httpClient.DefaultRequestHeaders.Remove("api-key");
        httpClient.DefaultRequestHeaders.Add("api-key", options.Value.ApiKey);
        httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

        var payload = new
        {
            sender = new { email = options.Value.SenderEmail, name = options.Value.SenderName },
            to = new[] { new { email = toEmail } },
            templateId,
            @params = templateParams
        };

        var response = await httpClient.PostAsJsonAsync("/v3/smtp/email", payload, cancellationToken);
        response.EnsureSuccessStatusCode();
        return response.Headers.TryGetValues("x-message-id", out var values) ? values.FirstOrDefault() : null;
    }

    public Task<string?> SendPushAsync(Guid userId, string subject, string body, CancellationToken cancellationToken)
    {
        return Task.FromResult<string?>(null);
    }
}
