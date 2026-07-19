using KromicFlow.Application.Abstractions;
using KromicFlow.Application.Common;
using KromicFlow.Application.DTOs.Instagram;
using KromicFlow.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace KromicFlow.Application.Features.Instagram.ConnectAccount;

internal sealed class ConnectAccountCommandHandler(
    IKromicFlowDbContext db,
    ILogger<ConnectAccountCommandHandler> logger) : IRequestHandler<ConnectAccountCommand, Result<InstagramAccountDto>>
{
    public async Task<Result<InstagramAccountDto>> Handle(ConnectAccountCommand request, CancellationToken cancellationToken)
    {
        logger.LogInformation("Connecting Instagram account {AccountId}", request.AccountId);

        var account = await db.InstagramAccounts
            .Include(x => x.User)
            .FirstOrDefaultAsync(x => x.Id == request.AccountId, cancellationToken);

        if (account is null)
        {
            logger.LogError("Instagram account not found: {AccountId}", request.AccountId);
            return Result<InstagramAccountDto>.Failure("Instagram account not found.");
        }

        if (account.IsConnected)
        {
            logger.LogWarning("Account {AccountId} is already connected", request.AccountId);
            return Result<InstagramAccountDto>.Failure("Account is already connected.");
        }

        // Validate token is still valid
        if (account.TokenExpiresUtc.HasValue && account.TokenExpiresUtc.Value < DateTime.UtcNow)
        {
            logger.LogError("Account {AccountId} token has expired", request.AccountId);
            return Result<InstagramAccountDto>.Failure("Account token has expired. Please re-authenticate.");
        }

        account.IsConnected = true;
        account.ConnectedAtUtc = DateTime.UtcNow;
        account.DisconnectedAtUtc = null;
        account.UpdatedUtc = DateTime.UtcNow;

        await db.SaveChangesAsync(cancellationToken);
        logger.LogInformation("Account {AccountId} marked as connected", request.AccountId);

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

        logger.LogInformation("Instagram account {AccountId} connected successfully", request.AccountId);
        return Result<InstagramAccountDto>.Success(dto);
    }
}
