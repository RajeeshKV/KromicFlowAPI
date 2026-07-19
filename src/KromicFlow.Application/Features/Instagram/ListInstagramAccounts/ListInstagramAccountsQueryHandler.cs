using KromicFlow.Application.Abstractions;
using KromicFlow.Application.DTOs.Instagram;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace KromicFlow.Application.Features.Instagram.ListInstagramAccounts;

internal sealed class ListInstagramAccountsQueryHandler(IKromicFlowDbContext db) : IRequestHandler<ListInstagramAccountsQuery, IReadOnlyList<InstagramAccountDto>>
{
    public async Task<IReadOnlyList<InstagramAccountDto>> Handle(ListInstagramAccountsQuery request, CancellationToken cancellationToken) =>
        await db.InstagramAccounts.Where(x => x.UserId == request.UserId)
            .OrderByDescending(x => x.ConnectedAtUtc)
            .Select(x => new InstagramAccountDto(
                x.Id,
                x.InstagramUserId,
                x.FacebookPageId,
                x.Username,
                x.DisplayName,
                x.ProfilePicture,
                x.IsConnected,
                x.ConnectedAtUtc,
                x.DisconnectedAtUtc,
                x.LastSyncUtc,
                x.TokenExpiresUtc,
                x.LastTokenRefreshUtc,
                x.TokenStatus,
                x.RefreshRequired
            ))
            .ToListAsync(cancellationToken);
}
