using KromicFlow.Application.Abstractions;
using KromicFlow.Application.Common;
using KromicFlow.Application.DTOs.Auth;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace KromicFlow.Application.Features.Auth.Refresh;

internal sealed class RefreshCommandHandler(
    IKromicFlowDbContext db,
    IRefreshTokenService refreshTokenService,
    IJwtTokenService jwtTokenService) : IRequestHandler<RefreshCommand, Result<AuthTokenDto>>
{
    public async Task<Result<AuthTokenDto>> Handle(RefreshCommand request, CancellationToken cancellationToken)
    {
        await using var transaction = await db.BeginTransactionAsync(cancellationToken);
        
        try
        {
            var hash = refreshTokenService.Hash(request.RefreshToken);
            var session = await db.Sessions.Include(x => x.User).Include(x => x.AdminUser).FirstOrDefaultAsync(x => x.SessionGuid == request.SessionGuid, cancellationToken);
            
            if (session is null || session.RevokedUtc is not null || session.ExpiresUtc <= DateTime.UtcNow || session.RefreshTokenHash != hash)
            {
                if (session is not null) session.RevokedUtc = DateTime.UtcNow;
                await db.SaveChangesAsync(cancellationToken);
                await transaction.CommitAsync(cancellationToken);
                return Result<AuthTokenDto>.Failure("Invalid refresh token.");
            }

            var refreshToken = refreshTokenService.CreateToken();
            session.RefreshTokenHash = refreshTokenService.Hash(refreshToken);
            session.LastSeenUtc = DateTime.UtcNow;
            await db.SaveChangesAsync(cancellationToken);

            var token = session.User is not null
                ? jwtTokenService.CreateUserToken(session.User, session)
                : jwtTokenService.CreateAdminToken(session.AdminUser!, session);

            await transaction.CommitAsync(cancellationToken);
            return Result<AuthTokenDto>.Success(token with { RefreshToken = refreshToken });
        }
        catch
        {
            await transaction.RollbackAsync(cancellationToken);
            throw;
        }
    }
}
