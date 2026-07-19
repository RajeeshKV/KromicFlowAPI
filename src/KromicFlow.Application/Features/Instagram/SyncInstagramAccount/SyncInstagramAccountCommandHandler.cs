using KromicFlow.Application.Abstractions;
using KromicFlow.Application.Common;
using KromicFlow.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace KromicFlow.Application.Features.Instagram.SyncInstagramAccount;

internal sealed class SyncInstagramAccountCommandHandler(IKromicFlowDbContext db, IMetaApiClient metaApiClient, IDataProtectionService dataProtectionService) : IRequestHandler<SyncInstagramAccountCommand, Result>
{
    public async Task<Result> Handle(SyncInstagramAccountCommand request, CancellationToken cancellationToken)
    {
        var account = await db.InstagramAccounts.FirstOrDefaultAsync(x => x.Id == request.InstagramAccountId && x.UserId == request.UserId, cancellationToken);
        if (account is null) return Result.Failure("Instagram account not found.");

        // Decrypt access token
        var accessToken = dataProtectionService.Unprotect(account.AccessTokenEncrypted);

        // Validate token expiry
        if (account.TokenExpiresUtc.HasValue && account.TokenExpiresUtc.Value < DateTime.UtcNow.AddDays(7))
        {
            // Token is expiring soon, attempt to refresh
            try
            {
                var refreshedToken = await metaApiClient.RefreshLongLivedTokenAsync(accessToken, cancellationToken);
                account.AccessTokenEncrypted = dataProtectionService.Protect(refreshedToken);
                account.TokenExpiresUtc = DateTime.UtcNow.AddDays(60);
                account.LastTokenRefreshUtc = DateTime.UtcNow;
                account.TokenStatus = "active";
                accessToken = refreshedToken;
            }
            catch
            {
                account.TokenStatus = "expired";
                await db.SaveChangesAsync(cancellationToken);
                return Result.Failure("Token has expired and could not be refreshed. Please re-authenticate.");
            }
        }

        // Refresh profile information
        try
        {
            var profile = await metaApiClient.RefreshInstagramAccountProfileAsync(accessToken, account.InstagramUserId, cancellationToken);
            account.Username = profile.Username;
            account.DisplayName = profile.Username;
            account.ProfilePicture = profile.ProfilePicture;
        }
        catch
        {
            // Profile refresh failed, but don't fail the entire sync
            account.RefreshRequired = true;
        }

        // Sync media
        await metaApiClient.SyncMediaAsync(account, cancellationToken);

        // Update sync timestamp
        account.LastSyncUtc = DateTime.UtcNow;
        account.RefreshRequired = false;
        account.UpdatedUtc = DateTime.UtcNow;

        await db.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}
