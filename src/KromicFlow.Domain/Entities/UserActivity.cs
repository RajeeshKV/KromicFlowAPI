using KromicFlow.Domain.Enums;

namespace KromicFlow.Domain.Entities;

public sealed class UserActivity : Entity
{
    public Guid UserId { get; set; }
    public User User { get; set; } = null!;
    public ActivityType Type { get; set; }
    public string Description { get; set; } = string.Empty;
    public string? MetadataJson { get; set; }
}
