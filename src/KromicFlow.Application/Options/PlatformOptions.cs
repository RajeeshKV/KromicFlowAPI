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
    public string EmailVerificationRedirectUrl { get; set; } = "https://yourdomain.com/verify-email";
}
