namespace KromicFlow.Api.Contracts.Auth;

public sealed record RefreshRequest(string RefreshToken, Guid SessionGuid);
