namespace KromicFlow.Domain.Entities;

public sealed class Session : Entity
{
    public Guid SessionGuid { get; set; } = Guid.NewGuid();
    public Guid? UserId { get; set; }
    public User? User { get; set; }
    public Guid? AdminUserId { get; set; }
    public AdminUser? AdminUser { get; set; }
    public string RefreshTokenHash { get; set; } = string.Empty;
    public string? DeviceName { get; set; }
    public string? Browser { get; set; }
    public string? OS { get; set; }
    public string? IPAddress { get; set; }
    public DateTime LastSeenUtc { get; set; } = DateTime.UtcNow;
    public DateTime ExpiresUtc { get; set; }
    public DateTime? RevokedUtc { get; set; }
}
