namespace KromicFlow.Domain.Entities;

public sealed class InstagramAccount : Entity
{
    public Guid UserId { get; set; }
    public User User { get; set; } = null!;
    public string InstagramUserId { get; set; } = string.Empty;
    public string Username { get; set; } = string.Empty;
    public string AccessTokenEncrypted { get; set; } = string.Empty;
    public bool RefreshRequired { get; set; }
    public DateTime ConnectedUtc { get; set; } = DateTime.UtcNow;
    public DateTime? LastSyncUtc { get; set; }
    public ICollection<Automation> Automations { get; set; } = [];
}
