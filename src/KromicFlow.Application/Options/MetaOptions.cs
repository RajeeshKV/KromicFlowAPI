using System.ComponentModel.DataAnnotations;

namespace KromicFlow.Application.Options;

public sealed class MetaOptions
{
    [Required]
    public string AppId { get; set; } = string.Empty;
    
    [Required]
    public string AppSecret { get; set; } = string.Empty;
    
    [Required]
    [Url]
    public string OAuthRedirectUri { get; set; } = string.Empty;
    
    [Required]
    [Url]
    public string FrontendRedirectUri { get; set; } = string.Empty;

    /// <summary>
    /// Mobile deep link URI for React Native/Expo clients.
    /// Example: kromicflow://auth/callback
    /// This must be registered in Meta's OAuth allowed redirect URIs.
    /// </summary>
    public string MobileRedirectUri { get; set; } = "kromicflow://auth/callback";
    
    [Required]
    public string WebhookVerifyToken { get; set; } = string.Empty;
    
    [Required]
    public string WebhookAppSecret { get; set; } = string.Empty;
    
    [Required]
    [Url]
    public string GraphApiBaseUrl { get; set; } = "https://graph.instagram.com/v20.0";
    
    [Required]
    [Url]
    public string ApiBaseUrl { get; set; } = "https://api.instagram.com";
}

