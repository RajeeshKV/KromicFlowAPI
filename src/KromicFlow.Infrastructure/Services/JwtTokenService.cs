using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using KromicFlow.Application.Abstractions;
using KromicFlow.Application.DTOs.Auth;
using KromicFlow.Application.Options;
using KromicFlow.Domain.Entities;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace KromicFlow.Infrastructure.Services;

public sealed class JwtTokenService(IOptions<JwtOptions> options) : IJwtTokenService
{
    public AuthTokenDto CreateUserToken(User user, Session session) => CreateToken(user.Id, session.SessionGuid, user.TokenVersion, user.Role.ToString());

    public AuthTokenDto CreateAdminToken(AdminUser admin, Session session) => CreateToken(admin.Id, session.SessionGuid, admin.TokenVersion, "Admin");

    private AuthTokenDto CreateToken(Guid subjectId, Guid sessionGuid, int tokenVersion, string role)
    {
        var now = DateTime.UtcNow;
        var expires = now.AddMinutes(options.Value.AccessTokenMinutes);
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(options.Value.Key));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub, subjectId.ToString()),
            new Claim("sid", sessionGuid.ToString()),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            new Claim("tv", tokenVersion.ToString()),
            new Claim(ClaimTypes.Role, role),
            new Claim(JwtRegisteredClaimNames.Iat, new DateTimeOffset(now).ToUnixTimeSeconds().ToString(), ClaimValueTypes.Integer64)
        };
        var token = new JwtSecurityToken(options.Value.Issuer, options.Value.Audience, claims, now, expires, credentials);
        return new AuthTokenDto(new JwtSecurityTokenHandler().WriteToken(token), string.Empty, expires, sessionGuid);
    }
}
