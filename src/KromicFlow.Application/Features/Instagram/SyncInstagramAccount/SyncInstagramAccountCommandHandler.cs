using KromicFlow.Application.Abstractions;
using KromicFlow.Application.Common;
using KromicFlow.Domain.Entities;
using KromicFlow.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace KromicFlow.Application.Features.Instagram.SyncInstagramAccount;

internal sealed class SyncInstagramAccountCommandHandler(
    IKromicFlowDbContext db,
    IMetaApiClient metaApiClient,
    IDataProtectionService dataProtectionService,
    ILogger<SyncInstagramAccountCommandHandler> logger) : IRequestHandler<SyncInstagramAccountCommand, Result>
{
    public async Task<Result> Handle(SyncInstagramAccountCommand request, CancellationToken cancellationToken)
    {
        logger.LogInformation("Starting Instagram account sync for {AccountId}", request.InstagramAccountId);

        var account = await db.InstagramAccounts.FirstOrDefaultAsync(x => x.Id == request.InstagramAccountId && x.UserId == request.UserId, cancellationToken);
        if (account is null)
        {
            logger.LogError("Instagram account not found: {AccountId}", request.InstagramAccountId);
            return Result.Failure("Instagram account not found.");
        }

        // Decrypt access token
        var accessToken = dataProtectionService.Unprotect(account.AccessTokenEncrypted);
        logger.LogInformation("Access token decrypted for account {InstagramUserId}", account.InstagramUserId);

        // Validate token expiry
        if (account.TokenExpiresUtc.HasValue && account.TokenExpiresUtc.Value < DateTime.UtcNow.AddDays(7))
        {
            // Token is expiring soon, attempt to refresh
            try
            {
                logger.LogInformation("Token expiring soon, attempting refresh");
                var refreshedToken = await metaApiClient.RefreshLongLivedTokenAsync(accessToken, cancellationToken);
                account.AccessTokenEncrypted = dataProtectionService.Protect(refreshedToken);
                account.TokenExpiresUtc = DateTime.UtcNow.AddDays(60);
                account.LastTokenRefreshUtc = DateTime.UtcNow;
                account.TokenStatus = "active";
                accessToken = refreshedToken;
                logger.LogInformation("Token refreshed successfully");
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Token refresh failed for account {AccountId}", account.Id);
                account.TokenStatus = "expired";
                await db.SaveChangesAsync(cancellationToken);
                return Result.Failure("Token has expired and could not be refreshed. Please re-authenticate.");
            }
        }

        // Refresh profile information
        try
        {
            logger.LogInformation("Refreshing profile for {InstagramUserId}", account.InstagramUserId);
            var profile = await metaApiClient.RefreshInstagramAccountProfileAsync(accessToken, account.InstagramUserId, cancellationToken);
            account.Username = profile.Username;
            account.DisplayName = profile.Username;
            account.ProfilePicture = profile.ProfilePicture;
            logger.LogInformation("Profile refreshed: {Username}", profile.Username);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Profile refresh failed for account {AccountId}", account.Id);
            account.RefreshRequired = true;
        }

        // Sync media
        try
        {
            logger.LogInformation("Requesting media from Instagram API for {InstagramUserId}", account.InstagramUserId);
            var mediaList = await metaApiClient.GetInstagramMediaAsync(accessToken, account.InstagramUserId, cancellationToken);
            logger.LogInformation("Retrieved {Count} media items from Instagram API", mediaList.Count);
            
            var existingMediaIds = mediaList.Select(m => m.Id).ToHashSet();
            
            // Get existing media for this account
            var existingMedia = await db.InstagramMedia
                .Where(x => x.InstagramAccountId == account.Id && !x.IsDeleted)
                .ToListAsync(cancellationToken);
            
            logger.LogInformation("Found {Count} existing media records in database", existingMedia.Count);
            
            int updatedCount = 0;
            int addedCount = 0;
            
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
                    updatedCount++;
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
                    addedCount++;
                }
            }
            
            logger.LogInformation("Media sync: {Updated} updated, {Added} added", updatedCount, addedCount);
            
            // Soft-delete media that no longer exists
            var mediaToDelete = existingMedia.Where(x => !existingMediaIds.Contains(x.InstagramMediaId)).ToList();
            foreach (var media in mediaToDelete)
            {
                media.IsDeleted = true;
                media.UpdatedUtc = DateTime.UtcNow;
            }
            
            if (mediaToDelete.Count > 0)
            {
                logger.LogInformation("Soft-deleted {Count} media items", mediaToDelete.Count);
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Media sync failed for account {AccountId}", account.Id);
            account.RefreshRequired = true;
            // Don't fail the entire sync, just mark for refresh
        }

        // Update sync timestamp
        account.LastSyncUtc = DateTime.UtcNow;
        account.RefreshRequired = false;
        account.UpdatedUtc = DateTime.UtcNow;

        await db.SaveChangesAsync(cancellationToken);
        logger.LogInformation("Instagram account sync completed successfully for {AccountId}", request.InstagramAccountId);
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
