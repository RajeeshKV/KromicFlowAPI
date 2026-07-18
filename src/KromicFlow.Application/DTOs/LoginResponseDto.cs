namespace KromicFlow.Application.DTOs.Auth;

public sealed record LoginResponseDto(AuthTokenDto Tokens, UserProfileDto Profile);
