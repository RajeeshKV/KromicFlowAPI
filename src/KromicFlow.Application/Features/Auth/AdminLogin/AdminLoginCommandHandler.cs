using KromicFlow.Application.Abstractions;
using KromicFlow.Application.Common;
using KromicFlow.Application.DTOs.Auth;
using KromicFlow.Application.Options;
using KromicFlow.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace KromicFlow.Application.Features.Auth.AdminLogin;

internal sealed class AdminLoginCommandHandler(
    IKromicFlowDbContext db,
    IPasswordHasher passwordHasher,
    IRefreshTokenService refreshTokenService,
    IJwtTokenService jwtTokenService,
    IAuditWriter auditWriter,
    IOptions<JwtOptions> jwtOptions) : IRequestHandler<AdminLoginCommand, Result<AdminLoginResponseDto>>
{
    public async Task<Result<AdminLoginResponseDto>> Handle(AdminLoginCommand request, CancellationToken cancellationToken)
    {
        var admin = await db.AdminUsers.FirstOrDefaultAsync(x => x.Username == request.Username, cancellationToken);
        if (admin is null || !admin.IsActive || !passwordHasher.Verify(request.Password, admin.PasswordHash))
        {
            return Result<AdminLoginResponseDto>.Failure("Invalid admin credentials.");
        }

        var refreshToken = refreshTokenService.CreateToken();
        var session = new Session
        {
            AdminUser = admin,
            RefreshTokenHash = refreshTokenService.Hash(refreshToken),
            DeviceName = request.DeviceName,
            Browser = request.Browser,
            OS = request.OS,
            IPAddress = request.IPAddress,
            ExpiresUtc = DateTime.UtcNow.AddDays(jwtOptions.Value.RefreshTokenDays)
        };

        admin.LastLoginUtc = DateTime.UtcNow;
        db.Sessions.Add(session);
        await db.SaveChangesAsync(cancellationToken);
        await auditWriter.WriteAsync("AdminLogin", nameof(AdminUser), admin.Id.ToString(), null, admin.Id, null, cancellationToken);

        var tokens = jwtTokenService.CreateAdminToken(admin, session) with { RefreshToken = refreshToken };
        return Result<AdminLoginResponseDto>.Success(new AdminLoginResponseDto(tokens, new AdminProfileDto(admin.Id, admin.Username, admin.Email)));
    }
}
