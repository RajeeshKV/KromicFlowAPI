namespace KromicFlow.Domain.Entities;

public sealed class AuditLog : Entity
{
    public Guid? ActorUserId { get; set; }
    public Guid? ActorAdminId { get; set; }
    public string Action { get; set; } = string.Empty;
    public string EntityName { get; set; } = string.Empty;
    public string? EntityId { get; set; }
    public string? DetailsJson { get; set; }
    public string? IPAddress { get; set; }
}
