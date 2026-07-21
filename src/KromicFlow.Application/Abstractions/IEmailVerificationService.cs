namespace KromicFlow.Application.Abstractions;

/// <summary>
/// Service for generating and managing email verification tokens
/// </summary>
public interface IEmailVerificationService
{
    /// <summary>
    /// Generates a random verification token (32 characters, URL-safe)
    /// </summary>
    string GenerateToken();

    /// <summary>
    /// Gets the token expiration time (24 hours from now)
    /// </summary>
    DateTime GetTokenExpirationTime();
}
