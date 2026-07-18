using KromicFlow.Application.Abstractions;
using KromicFlow.Application.Common;
using KromicFlow.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace KromicFlow.Application.Features.Webhooks.PersistMetaWebhook;

internal sealed class PersistMetaWebhookCommandHandler(IKromicFlowDbContext db) : IRequestHandler<PersistMetaWebhookCommand, Result>
{
    public async Task<Result> Handle(PersistMetaWebhookCommand request, CancellationToken cancellationToken)
    {
        if (await db.WebhookEvents.AnyAsync(x => x.EventId == request.EventId, cancellationToken)) return Result.Success();
        db.WebhookEvents.Add(new WebhookEvent { EventId = request.EventId, Payload = request.Payload });
        await db.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}
