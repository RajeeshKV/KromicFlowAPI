using System.ComponentModel.DataAnnotations;

namespace KromicFlow.Application.Options;

public sealed class JwtOptions
{
    [Required]
    [MinLength(32, ErrorMessage = "JWT Key must be at least 32 characters")]
    public string Key { get; set; } = string.Empty;
    
    [Required]
    public string Issuer { get; set; } = "KromicFlow";
    
    [Required]
    public string Audience { get; set; } = "KromicFlow";
    
    [Range(1, 60)]
    public int AccessTokenMinutes { get; set; } = 15;
    
    [Range(1, 365)]
    public int RefreshTokenDays { get; set; } = 30;
}
