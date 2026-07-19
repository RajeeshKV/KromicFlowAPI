using KromicFlow.Application.Abstractions;
using KromicFlow.Application.Common;
using KromicFlow.Domain.Entities;
using KromicFlow.Domain.Enums;
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
        try
        {
            var mediaList = await metaApiClient.GetInstagramMediaAsync(accessToken, account.InstagramUserId, cancellationToken);
            
            var existingMediaIds = mediaList.Select(m => m.Id).ToHashSet();
            
            // Get existing media for this account
            var existingMedia = await db.InstagramMedia
                .Where(x => x.InstagramAccountId == account.Id && !x.IsDeleted)
                .ToListAsync(cancellationToken);
            
            // Update existing media or add new media
            foreach (var mediaItem in mediaList)
            {
                var existing = existingMedia.FirstOrDefault(x => x.InstagramMediaId == mediaItem.Id);
                
                if (existing != null)
                {
                    // Update existing media
                    existing.Caption = mediaItem.Caption;
                    existing.ThumbnailUrl = mediaItem.ThumbnailUrl;
                    existing.MediaUrl = mediaItem.MediaUrl;
                    existing.Permalink = mediaItem.Permalink;
                    existing.LikeCount = mediaItem.LikeCount;
                    existing.CommentsCount = mediaItem.CommentsCount;
                    existing.LastSyncedAtUtc = DateTime.UtcNow;
                    existing.UpdatedUtc = DateTime.UtcNow;
                }
                else
                {
                    // Add new media
                    var mediaType = ParseMediaType(mediaItem.MediaType);
                    db.InstagramMedia.Add(new InstagramMedia
                    {
                        InstagramAccountId = account.Id,
                        InstagramMediaId = mediaItem.Id,
                        MediaType = mediaType,
                        Caption = mediaItem.Caption,
                        ThumbnailUrl = mediaItem.ThumbnailUrl,
                        MediaUrl = mediaItem.MediaUrl,
                        Permalink = mediaItem.Permalink,
                        PostedAtUtc = mediaItem.PostedAtUtc,
                        LikeCount = mediaItem.LikeCount,
                        CommentsCount = mediaItem.CommentsCount,
                        LastSyncedAtUtc = DateTime.UtcNow,
                        CreatedUtc = DateTime.UtcNow,
                        UpdatedUtc = DateTime.UtcNow
                    });
                }
            }
            
            // Soft-delete media that no longer exists
            var mediaToDelete = existingMedia.Where(x => !existingMediaIds.Contains(x.InstagramMediaId)).ToList();
            foreach (var media in mediaToDelete)
            {
                media.IsDeleted = true;
                media.UpdatedUtc = DateTime.UtcNow;
            }
        }
        catch
        {
            // Media sync failed, but don't fail the entire sync
            account.RefreshRequired = true;
        }

        // Sync media (existing implementation)
        await metaApiClient.SyncMediaAsync(account, cancellationToken);

        // Update sync timestamp
        account.LastSyncUtc = DateTime.UtcNow;
        account.RefreshRequired = false;
        account.UpdatedUtc = DateTime.UtcNow;

        await db.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }

    private static MediaType ParseMediaType(string mediaType)
    {
        return mediaType.ToLowerInvariant() switch
        {
            "image" => MediaType.Image,
            "video" => MediaType.Video,
            "carousel_album" => MediaType.Carousel,
            "reels" => MediaType.Reel,
            _ => MediaType.Image
        };
    }
}
