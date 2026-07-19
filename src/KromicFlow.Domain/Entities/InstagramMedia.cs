using KromicFlow.Domain.Enums;

namespace KromicFlow.Domain.Entities;

public sealed class InstagramMedia : Entity
{
    public Guid InstagramAccountId { get; set; }
    public InstagramAccount InstagramAccount { get; set; } = null!;
    
    public string InstagramMediaId { get; set; } = string.Empty;
    public MediaType MediaType { get; set; }
    public string Caption { get; set; } = string.Empty;
    public string ThumbnailUrl { get; set; } = string.Empty;
    public string MediaUrl { get; set; } = string.Empty;
    public string Permalink { get; set; } = string.Empty;
    public DateTime PostedAtUtc { get; set; }
    public int LikeCount { get; set; }
    public int CommentsCount { get; set; }
    public bool IsDeleted { get; set; }
    public DateTime? LastSyncedAtUtc { get; set; }
    
    public ICollection<AutomationMedia> AutomationMedia { get; set; } = [];
}
