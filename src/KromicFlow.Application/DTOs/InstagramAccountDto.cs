namespace KromicFlow.Application.DTOs.Instagram;

public sealed record InstagramAccountDto(Guid Id, string InstagramUserId, string Username, bool RefreshRequired, DateTime ConnectedUtc, DateTime? LastSyncUtc);
