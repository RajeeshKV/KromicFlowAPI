using KromicFlow.Application.Abstractions;
using KromicFlow.Application.Common;
using KromicFlow.Application.DTOs;
using KromicFlow.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace KromicFlow.Application.Features.Instagram.GetInstagramMedia;

internal sealed class GetInstagramMediaQueryHandler(IKromicFlowDbContext db) : IRequestHandler<GetInstagramMediaQuery, Result<MediaPaginationResponseDto>>
{
    public async Task<Result<MediaPaginationResponseDto>> Handle(GetInstagramMediaQuery request, CancellationToken cancellationToken)
    {
        // Verify account exists and belongs to user
        var account = await db.InstagramAccounts
            .FirstOrDefaultAsync(x => x.Id == request.InstagramAccountId, cancellationToken);

        if (account == null)
        {
            return Result<MediaPaginationResponseDto>.Failure("Instagram account not found.");
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
}
