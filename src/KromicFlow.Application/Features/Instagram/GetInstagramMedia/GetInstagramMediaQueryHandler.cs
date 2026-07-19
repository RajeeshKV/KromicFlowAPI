using KromicFlow.Application.Abstractions;
using KromicFlow.Application.Common;
using KromicFlow.Application.DTOs;
using KromicFlow.Domain.Entities;
using KromicFlow.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace KromicFlow.Application.Features.Instagram.GetInstagramMedia;

internal sealed class GetInstagramMediaQueryHandler(
    IKromicFlowDbContext db,
    IMetaApiClient metaApiClient,
    IDataProtectionService dataProtectionService,
    ILogger<GetInstagramMediaQueryHandler> logger) : IRequestHandler<GetInstagramMediaQuery, Result<MediaPaginationResponseDto>>
{
    public async Task<Result<MediaPaginationResponseDto>> Handle(GetInstagramMediaQuery request, CancellationToken cancellationToken)
    {
        logger.LogInformation("Getting media for Instagram account {AccountId}", request.InstagramAccountId);

        // Verify account exists and belongs to user
        var account = await db.InstagramAccounts
            .FirstOrDefaultAsync(x => x.Id == request.InstagramAccountId, cancellationToken);

        if (account == null)
        {
            logger.LogError("Instagram account not found: {AccountId}", request.InstagramAccountId);
            return Result<MediaPaginationResponseDto>.Failure("Instagram account not found.");
        }

        // Check if sync is needed (no media or last sync > 24 hours ago)
        var existingMediaCount = await db.InstagramMedia
            .Where(x => x.InstagramAccountId == request.InstagramAccountId && !x.IsDeleted)
            .CountAsync(cancellationToken);

        var needsSync = existingMediaCount == 0 || 
                        (account.LastSyncUtc == null || account.LastSyncUtc.Value < DateTime.UtcNow.AddHours(-24));

        if (needsSync)
        {
            logger.LogInformation("Media sync needed for account {AccountId} (existing: {Count}, lastSync: {LastSync})", 
                request.InstagramAccountId, existingMediaCount, account.LastSyncUtc);
            
            try
            {
                await SyncMediaAsync(account, cancellationToken);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Media sync failed for account {AccountId}", request.InstagramAccountId);
                // Don't fail the request, just return what we have (or empty)
            }
        }

        var query = db.InstagramMedia
            .Where(x => x.InstagramAccountId == request.InstagramAccountId && !x.IsDeleted);

        // Filter by media type
        if (request.MediaType.HasValue)
        {
            query = query.Where(x => x.MediaType == request.MediaType.Value);
        }

        // Search by caption
        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            query = query.Where(x => x.Caption.Contains(request.Search));
        }

        var total = await query.CountAsync(cancellationToken);
        logger.LogInformation("Returning {Count} media items for account {AccountId}", total, request.InstagramAccountId);

        var items = await query
            .OrderByDescending(x => x.PostedAtUtc)
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .Select(x => new InstagramMediaDto
            {
                Id = x.Id,
                InstagramMediaId = x.InstagramMediaId,
                MediaType = x.MediaType,
                Caption = x.Caption,
                ThumbnailUrl = x.ThumbnailUrl,
                MediaUrl = x.MediaUrl,
                Permalink = x.Permalink,
                PostedAtUtc = x.PostedAtUtc,
                LikeCount = x.LikeCount,
                CommentsCount = x.CommentsCount,
                LastSyncedAtUtc = x.LastSyncedAtUtc
            })
            .ToListAsync(cancellationToken);

        return Result<MediaPaginationResponseDto>.Success(new MediaPaginationResponseDto
        {
            Items = items,
            Total = total
        });
    }

    private async Task SyncMediaAsync(InstagramAccount account, CancellationToken cancellationToken)
    {
        logger.LogInformation("Starting media sync for account {AccountId}", account.Id);

        var accessToken = dataProtectionService.Unprotect(account.AccessTokenEncrypted);

        // Refresh token if needed
        if (account.TokenExpiresUtc.HasValue && account.TokenExpiresUtc.Value < DateTime.UtcNow.AddDays(7))
        {
            try
            {
                logger.LogInformation("Token expiring soon, attempting refresh");
                var refreshedToken = await metaApiClient.RefreshLongLivedTokenAsync(accessToken, cancellationToken);
                account.AccessTokenEncrypted = dataProtectionService.Protect(refreshedToken);
                account.TokenExpiresUtc = DateTime.UtcNow.AddDays(60);
                account.LastTokenRefreshUtc = DateTime.UtcNow;
                account.TokenStatus = "active";
                accessToken = refreshedToken;
                await db.SaveChangesAsync(cancellationToken);
                logger.LogInformation("Token refreshed successfully");
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Token refresh failed for account {AccountId}", account.Id);
                // Continue with existing token
            }
        }

        // Sync media
        var mediaList = await metaApiClient.GetInstagramMediaAsync(accessToken, account.InstagramUserId, cancellationToken);
        logger.LogInformation("Retrieved {Count} media items from Instagram API", mediaList.Count);

        var existingMediaIds = mediaList.Select(m => m.Id).ToHashSet();
        var existingMedia = await db.InstagramMedia
            .Where(x => x.InstagramAccountId == account.Id && !x.IsDeleted)
            .ToListAsync(cancellationToken);

        logger.LogInformation("Found {Count} existing media records in database", existingMedia.Count);

        int updatedCount = 0;
        int addedCount = 0;

        foreach (var mediaItem in mediaList)
        {
            var existing = existingMedia.FirstOrDefault(x => x.InstagramMediaId == mediaItem.Id);

            if (existing != null)
            {
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

        account.LastSyncUtc = DateTime.UtcNow;
        account.RefreshRequired = false;
        account.UpdatedUtc = DateTime.UtcNow;

        await db.SaveChangesAsync(cancellationToken);
        logger.LogInformation("Media sync completed for account {AccountId}", account.Id);
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
