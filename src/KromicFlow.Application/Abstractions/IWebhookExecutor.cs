using KromicFlow.Domain.Entities;

namespace KromicFlow.Application.Abstractions;

/// <summary>
/// Executes a single WebhookEvent — resolves the matching automation and fires
/// the configured public reply and/or DM. Updates the event status in-place;
/// the caller is responsible for calling SaveChangesAsync.
/// </summary>
public interface IWebhookExecutor
{
    Task ExecuteAsync(WebhookEvent webhookEvent, CancellationToken cancellationToken);
}
