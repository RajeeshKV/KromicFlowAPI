using KromicFlow.Domain.Enums;

namespace KromicFlow.Application.DTOs;

public sealed record InstagramMediaDto
{
    public Guid Id { get; init; }
    public string InstagramMediaId { get; init; } = string.Empty;
    public MediaType MediaType { get; init; }
    public string Caption { get; init; } = string.Empty;
    public string ThumbnailUrl { get; init; } = string.Empty;
    public string MediaUrl { get; init; } = string.Empty;
    public string Permalink { get; init; } = string.Empty;
    public DateTime PostedAtUtc { get; init; }
    public int LikeCount { get; init; }
    public int CommentsCount { get; init; }
    public DateTime? LastSyncedAtUtc { get; init; }
}
