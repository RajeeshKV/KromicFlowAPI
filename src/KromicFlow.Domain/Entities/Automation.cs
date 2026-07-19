using KromicFlow.Domain.Enums;

namespace KromicFlow.Domain.Entities;

public sealed class Automation : Entity
{
    public Guid InstagramAccountId { get; set; }
    public InstagramAccount InstagramAccount { get; set; } = null!;
    public string Name { get; set; } = string.Empty;
    public AutomationScope Scope { get; set; } = AutomationScope.SpecificPosts;
    public AutomationTriggerType TriggerType { get; set; }
    public string KeywordsJson { get; set; } = "[]";
    public string? PublicReply { get; set; }
    public string? PrivateReply { get; set; }
    public bool Enabled { get; set; } = true;
    public int CooldownSeconds { get; set; }
    public int Priority { get; set; }
    public DateTime? ActiveFromUtc { get; set; }
    public DateTime? ActiveUntilUtc { get; set; }
    
    public ICollection<AutomationMedia> AutomationMedia { get; set; } = [];
}
