namespace KromicFlow.Api.Contracts.Auth;

public sealed record AdminBootstrapRequest(string BootstrapKey, string Username, string Email, string Password);
