namespace KromicFlow.Application.Options;

public sealed class JwtOptions
{
    public string Key { get; set; } = string.Empty;
    public string Issuer { get; set; } = "KromicFlow";
    public string Audience { get; set; } = "KromicFlow";
    public int AccessTokenMinutes { get; set; } = 15;
    public int RefreshTokenDays { get; set; } = 30;
}
