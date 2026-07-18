namespace KromicFlow.Domain.Entities;

public sealed class UserRestriction : Entity
{
    public Guid UserId { get; set; }
    public User User { get; set; } = null!;
    public bool LoginBlocked { get; set; }
    public bool AutomationBlocked { get; set; }
    public bool NotificationBlocked { get; set; }
    public string? Reason { get; set; }
    public Guid? SetByAdminId { get; set; }
}
