using System.Security.Cryptography;
using System.Text;
using KromicFlow.Application.Features.Webhooks.PersistMetaWebhook;
using KromicFlow.Application.Options;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace KromicFlow.Api.Controllers;

[Route("api/v1/webhooks/meta")]
public sealed class MetaWebhooksController(
    IMediator mediator, 
    IOptions<MetaOptions> options,
    ILogger<MetaWebhooksController> logger) : ApiControllerBase
{
    [HttpGet]
    public IActionResult Verify([FromQuery(Name = "hub.mode")] string mode, [FromQuery(Name = "hub.verify_token")] string token, [FromQuery(Name = "hub.challenge")] string challenge)
    {
        logger.LogInformation("Webhook verification request - Mode: {Mode}, Token: {Token}", mode, token);
        var isValid = mode == "subscribe" && token == options.Value.WebhookVerifyToken;
        logger.LogInformation("Webhook verification result: {Result}", isValid ? "Success" : "Failed");
        return isValid ? Content(challenge) : Unauthorized();
    }

    [HttpPost]
    public async Task<IActionResult> Receive(CancellationToken cancellationToken)
    {
        using var reader = new StreamReader(Request.Body, Encoding.UTF8);
        var payload = await reader.ReadToEndAsync(cancellationToken);
        
        logger.LogInformation("Received Meta webhook - Payload length: {Length}", payload.Length);
        logger.LogInformation("Webhook payload: {Payload}", payload);
        
        var signatureHeader = Request.Headers["X-Hub-Signature-256"].ToString();
        logger.LogInformation("Webhook signature header: {Signature}", signatureHeader);
        
        if (!IsValidSignature(payload, signatureHeader))
        {
            logger.LogWarning("Webhook signature validation failed");
            return Unauthorized();
        }
        
        logger.LogInformation("Webhook signature validated successfully");
        
        var eventId = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(payload)));
        logger.LogInformation("Generated webhook event ID: {EventId}", eventId);
        
        var result = await mediator.Send(new PersistMetaWebhookCommand(eventId, payload), cancellationToken);
        
        logger.LogInformation("Webhook processing result: {Result}", result.Succeeded ? "Success" : "Failed");
        if (!result.Succeeded)
        {
            logger.LogError("Webhook processing failed: {Error}", result.Error);
        }
        
        return FromResult(result);
    }

    private bool IsValidSignature(string payload, string? signatureHeader)
    {
        if (string.IsNullOrWhiteSpace(options.Value.WebhookAppSecret))
        {
            logger.LogWarning("Webhook app secret is not configured");
            return false;
        }
        
        if (string.IsNullOrWhiteSpace(signatureHeader) || !signatureHeader.StartsWith("sha256=", StringComparison.OrdinalIgnoreCase))
        {
            logger.LogWarning("Invalid signature header format");
            return false;
        }
        
        var received = signatureHeader[7..];
        using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(options.Value.WebhookAppSecret));
        var expected = Convert.ToHexString(hmac.ComputeHash(Encoding.UTF8.GetBytes(payload))).ToLowerInvariant();
        var isValid = CryptographicOperations.FixedTimeEquals(Encoding.UTF8.GetBytes(received), Encoding.UTF8.GetBytes(expected));
        
        logger.LogInformation("Signature validation - Received: {Received}, Expected: {Expected}, Valid: {IsValid}", received, expected, isValid);
        
        return isValid;
    }
}
