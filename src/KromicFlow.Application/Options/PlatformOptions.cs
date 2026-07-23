using System.ComponentModel.DataAnnotations;

namespace KromicFlow.Application.Options;

public sealed class PlatformOptions
{
    [Required]
    public string TermsVersion { get; set; } = "2026-07-18";
    
    [Required]
    public string DefaultPlanCode { get; set; } = "free";
    
    [Required]
    public string AdminBootstrapKey { get; set; } = string.Empty;

    /// <summary>
    /// Frontend URL for email verification redirect
    /// Example: https://yourdomain.com/verify-email
    /// </summary>
    public string EmailVerificationRedirectUrl { get; set; } = "https://flow.kromic.in/verify-email";

    /// <summary>
    /// Mobile deep linking scheme (e.g., "kromicflow")
    /// Used for OAuth callback deep links: kromicflow://auth/callback?code=...&state=...
    /// </summary>
    public string MobileDeepLinkScheme { get; set; } = "kromicflow";

    /// <summary>
    /// Mobile OAuth callback path (e.g., "auth/callback")
    /// Combined with scheme to form: {scheme}://{path}
    /// </summary>
    public string MobileOAuthCallbackPath { get; set; } = "auth/callback";
}
