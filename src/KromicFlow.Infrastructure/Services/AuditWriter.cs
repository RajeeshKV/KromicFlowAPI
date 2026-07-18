using KromicFlow.Application.Abstractions;
using KromicFlow.Domain.Entities;

namespace KromicFlow.Infrastructure.Services;

public sealed class AuditWriter(IKromicFlowDbContext db) : IAuditWriter
{
    public async Task WriteAsync(string action, string entityName, string? entityId, Guid? actorUserId, Guid? actorAdminId, string? detailsJson, CancellationToken cancellationToken)
    {
        db.AuditLogs.Add(new AuditLog
        {
            Action = action,
            EntityName = entityName,
            EntityId = entityId,
            ActorUserId = actorUserId,
            ActorAdminId = actorAdminId,
            DetailsJson = detailsJson
        });
        await db.SaveChangesAsync(cancellationToken);
    }
}
