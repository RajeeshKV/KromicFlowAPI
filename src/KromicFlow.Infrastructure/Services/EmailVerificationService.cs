using System.Security.Cryptography;
using System.Text;

namespace KromicFlow.Infrastructure.Services;

/// <summary>
/// Service for generating and validating email verification tokens
/// </summary>
public interface IEmailVerificationService
{
    /// <summary>
    /// Generate a secure email verification token
    /// </summary>
    string GenerateToken();

    /// <summary>
    /// Get the expiration time for a verification token (24 hours from now)
    /// </summary>
    DateTime GetTokenExpirationTime();
}

public sealed class EmailVerificationService : IEmailVerificationService
{
    /// <summary>
    /// Generate a cryptographically secure random token (32 bytes = 256 bits)
    /// </summary>
    public string GenerateToken()
    {
        var randomBytes = new byte[32];
        using (var rng = RandomNumberGenerator.Create())
        {
            rng.GetBytes(randomBytes);
        }
        return Convert.ToBase64String(randomBytes)
            .Replace("/", "-")
            .Replace("+", "_")
            .Replace("=", "");
    }

    /// <summary>
    /// Get token expiration time: 24 hours from now
    /// </summary>
    public DateTime GetTokenExpirationTime()
    {
        return DateTime.UtcNow.AddHours(24);
    }
}
