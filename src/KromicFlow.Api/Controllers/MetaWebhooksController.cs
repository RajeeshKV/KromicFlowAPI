using System.Security.Cryptography;
using System.Text;
using KromicFlow.Application.Features.Webhooks.PersistMetaWebhook;
using KromicFlow.Application.Options;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace KromicFlow.Api.Controllers;

[Route("api/v1/webhooks/meta")]
public sealed class MetaWebhooksController(IMediator mediator, IOptions<MetaOptions> options) : ApiControllerBase
{
    [HttpGet]
    public IActionResult Verify([FromQuery(Name = "hub.mode")] string mode, [FromQuery(Name = "hub.verify_token")] string token, [FromQuery(Name = "hub.challenge")] string challenge)
    {
        return mode == "subscribe" && token == options.Value.WebhookVerifyToken ? Content(challenge) : Unauthorized();
    }

    [HttpPost]
    public async Task<IActionResult> Receive(CancellationToken cancellationToken)
    {
        using var reader = new StreamReader(Request.Body, Encoding.UTF8);
        var payload = await reader.ReadToEndAsync(cancellationToken);
        if (!IsValidSignature(payload, Request.Headers["X-Hub-Signature-256"])) return Unauthorized();
        var eventId = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(payload)));
        return FromResult(await mediator.Send(new PersistMetaWebhookCommand(eventId, payload), cancellationToken));
    }

    private bool IsValidSignature(string payload, string? signatureHeader)
    {
        if (string.IsNullOrWhiteSpace(options.Value.WebhookAppSecret)) return false;
        if (string.IsNullOrWhiteSpace(signatureHeader) || !signatureHeader.StartsWith("sha256=", StringComparison.OrdinalIgnoreCase)) return false;
        var received = signatureHeader[7..];
        using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(options.Value.WebhookAppSecret));
        var expected = Convert.ToHexString(hmac.ComputeHash(Encoding.UTF8.GetBytes(payload))).ToLowerInvariant();
        return CryptographicOperations.FixedTimeEquals(Encoding.UTF8.GetBytes(received), Encoding.UTF8.GetBytes(expected));
    }
}
