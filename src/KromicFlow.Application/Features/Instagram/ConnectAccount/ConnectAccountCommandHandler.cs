using KromicFlow.Application.Abstractions;
using KromicFlow.Application.Common;
using KromicFlow.Application.DTOs.Instagram;
using KromicFlow.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace KromicFlow.Application.Features.Instagram.ConnectAccount;

internal sealed class ConnectAccountCommandHandler(IKromicFlowDbContext db) : IRequestHandler<ConnectAccountCommand, Result<InstagramAccountDto>>
{
    public async Task<Result<InstagramAccountDto>> Handle(ConnectAccountCommand request, CancellationToken cancellationToken)
    {
        var account = await db.InstagramAccounts
            .Include(x => x.User)
            .FirstOrDefaultAsync(x => x.Id == request.AccountId, cancellationToken);

        if (account is null)
        {
            return Result<InstagramAccountDto>.Failure("Instagram account not found.");
        }

        if (account.IsConnected)
        {
            return Result<InstagramAccountDto>.Failure("Account is already connected.");
        }

        // Validate token is still valid
        if (account.TokenExpiresUtc.HasValue && account.TokenExpiresUtc.Value < DateTime.UtcNow)
        {
            return Result<InstagramAccountDto>.Failure("Account token has expired. Please re-authenticate.");
        }

        account.IsConnected = true;
        account.ConnectedAtUtc = DateTime.UtcNow;
        account.DisconnectedAtUtc = null;
        account.UpdatedUtc = DateTime.UtcNow;

        await db.SaveChangesAsync(cancellationToken);

        var dto = new InstagramAccountDto(
            account.Id,
            account.InstagramUserId,
            account.FacebookPageId,
            account.Username,
            account.DisplayName,
            account.ProfilePicture,
            account.IsConnected,
            account.ConnectedAtUtc,
            account.DisconnectedAtUtc,
            account.LastSyncUtc,
            account.TokenExpiresUtc,
            account.LastTokenRefreshUtc,
            account.TokenStatus,
            account.RefreshRequired
        );

        return Result<InstagramAccountDto>.Success(dto);
    }
}
