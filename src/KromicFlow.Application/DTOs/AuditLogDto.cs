namespace KromicFlow.Application.DTOs.Admin;

public sealed record AuditLogDto(Guid Id, string Action, string EntityName, string? EntityId, string? DetailsJson, DateTime CreatedUtc);
