namespace KromicFlow.Domain.Entities;

public sealed class InstagramAccount : Entity
{
    public Guid UserId { get; set; }
    public User User { get; set; } = null!;
    
    // Instagram account identifiers
    // InstagramUserId holds the IG_ID (real Instagram Business Account ID) — this is what
    // Meta puts in entry.id of webhook payloads and is used for all API calls.
    public string InstagramUserId { get; set; } = string.Empty;
    // InstagramScopedId holds the app-scoped ID (IGSID) returned by /me?fields=id.
    // It differs per app and is NOT used for webhook lookup — stored for reference only.
    public string? InstagramScopedId { get; set; }
    public string FacebookPageId { get; set; } = string.Empty;
    
    // Profile information
    public string Username { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string ProfilePicture { get; set; } = string.Empty;
    
    // Authentication
    public string AccessTokenEncrypted { get; set; } = string.Empty;
    public DateTime? TokenExpiresUtc { get; set; }
    public DateTime? LastTokenRefreshUtc { get; set; }
    public string TokenStatus { get; set; } = "active"; // active, expired, revoked, invalid
    
    // Connection state
    public bool IsConnected { get; set; }
    public DateTime? ConnectedAtUtc { get; set; }
    public DateTime? DisconnectedAtUtc { get; set; }
    
    // Synchronization
    public DateTime? LastSyncUtc { get; set; }
    public bool RefreshRequired { get; set; }
    
    public ICollection<Automation> Automations { get; set; } = [];
}
