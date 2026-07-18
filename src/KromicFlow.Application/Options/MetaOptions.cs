namespace KromicFlow.Application.Options;

public sealed class MetaOptions
{
    public string AppId { get; set; } = string.Empty;
    public string AppSecret { get; set; } = string.Empty;
    public string OAuthRedirectUri { get; set; } = string.Empty;
    public string WebhookVerifyToken { get; set; } = string.Empty;
    public string WebhookAppSecret { get; set; } = string.Empty;
    public string GraphApiBaseUrl { get; set; } = "https://graph.facebook.com/v20.0";
}
