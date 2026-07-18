using System.Security.Cryptography;
using System.Text;
using KromicFlow.Application.Abstractions;

namespace KromicFlow.Infrastructure.Services;

public sealed class RefreshTokenService : IRefreshTokenService
{
    public string CreateToken()
    {
        var bytes = RandomNumberGenerator.GetBytes(64);
        return Convert.ToBase64String(bytes);
    }

    public string Hash(string token)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(token));
        return Convert.ToHexString(bytes);
    }
}
