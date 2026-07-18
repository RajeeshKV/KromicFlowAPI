using KromicFlow.Application.Common;
using MediatR;

namespace KromicFlow.Application.Features.Webhooks.PersistMetaWebhook;

public sealed record PersistMetaWebhookCommand(string EventId, string Payload) : IRequest<Result>;
