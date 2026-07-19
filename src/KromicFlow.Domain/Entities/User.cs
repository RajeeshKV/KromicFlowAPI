using KromicFlow.Domain.Enums;

namespace KromicFlow.Domain.Entities;

public sealed class User : Entity
{
    public string Email { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public UserRole Role { get; set; } = UserRole.User;
    public int TokenVersion { get; set; }
    public bool IsActive { get; set; } = true;
    public bool MarketingEmailEnabled { get; set; } = true;
    public bool MarketingPushEnabled { get; set; } = true;
    public Guid PlanId { get; set; }
    public Plan Plan { get; set; } = null!;
    public UserRestriction? Restriction { get; set; }
    public ICollection<Session> Sessions { get; set; } = [];
    public ICollection<InstagramAccount> InstagramAccounts { get; set; } = [];
    public ICollection<Automation> Automations { get; set; } = [];
}
