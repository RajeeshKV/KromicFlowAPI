namespace KromicFlow.Application.DTOs.Auth;

public sealed record AuthTokenDto(string AccessToken, string RefreshToken, DateTime ExpiresUtc, Guid SessionGuid);
