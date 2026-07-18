namespace KromicFlow.Application.Abstractions;

public interface IAuditWriter
{
    Task WriteAsync(string action, string entityName, string? entityId, Guid? actorUserId, Guid? actorAdminId, string? detailsJson, CancellationToken cancellationToken);
}
