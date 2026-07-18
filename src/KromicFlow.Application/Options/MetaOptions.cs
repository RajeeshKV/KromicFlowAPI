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
    public string WebhookVerifyToken { get; set; } = string.Empty;
    
    [Required]
    public string WebhookAppSecret { get; set; } = string.Empty;
    
    [Required]
    [Url]
    public string GraphApiBaseUrl { get; set; } = "https://graph.instagram.com/v20.0";
}
