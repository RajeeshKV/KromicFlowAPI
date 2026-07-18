using KromicFlow.Application.Abstractions;
using Microsoft.AspNetCore.DataProtection;

namespace KromicFlow.Infrastructure.Services;

public sealed class DataProtectionService(IDataProtectionProvider dataProtectionProvider) : IDataProtectionService
{
    private readonly IDataProtector _protector = dataProtectionProvider.CreateProtector("KromicFlow.OAuthTokens");

    public string Protect(string plaintext)
    {
        if (string.IsNullOrWhiteSpace(plaintext))
            throw new ArgumentException("Plaintext cannot be empty", nameof(plaintext));

        return _protector.Protect(plaintext);
    }

    public string Unprotect(string protectedText)
    {
        if (string.IsNullOrWhiteSpace(protectedText))
            throw new ArgumentException("Protected text cannot be empty", nameof(protectedText));

        return _protector.Unprotect(protectedText);
    }
}
