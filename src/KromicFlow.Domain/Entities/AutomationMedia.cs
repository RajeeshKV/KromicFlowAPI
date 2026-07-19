namespace KromicFlow.Domain.Entities;

public sealed class AutomationMedia : Entity
{
    public Guid AutomationId { get; set; }
    public Automation Automation { get; set; } = null!;
    
    public Guid InstagramMediaId { get; set; }
    public InstagramMedia InstagramMedia { get; set; } = null!;
    
    public DateTime CreatedAtUtc { get; set; }
}
