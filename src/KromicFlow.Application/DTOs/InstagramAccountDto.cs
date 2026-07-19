namespace KromicFlow.Application.DTOs.Instagram;

public sealed record InstagramAccountDto(
    Guid Id,
    string InstagramUserId,
    string FacebookPageId,
    string Username,
    string DisplayName,
    string ProfilePicture,
    bool IsConnected,
    DateTime? ConnectedAtUtc,
    DateTime? DisconnectedAtUtc,
    DateTime? LastSyncUtc,
    DateTime? TokenExpiresUtc,
    DateTime? LastTokenRefreshUtc,
    string TokenStatus,
    bool RefreshRequired
);
