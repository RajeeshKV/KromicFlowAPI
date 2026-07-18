namespace KromicFlow.Domain.Entities;

public sealed class AdminUser : Entity
{
    public string Username { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
    public int TokenVersion { get; set; }
    public DateTime? LastLoginUtc { get; set; }
}
