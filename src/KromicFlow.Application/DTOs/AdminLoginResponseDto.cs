namespace KromicFlow.Application.DTOs.Auth;

public sealed record AdminLoginResponseDto(AuthTokenDto Tokens, AdminProfileDto Profile);
