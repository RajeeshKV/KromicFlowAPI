using KromicFlow.Application.Abstractions;
using KromicFlow.Application.DTOs.Instagram;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace KromicFlow.Application.Features.Instagram.ListInstagramAccounts;

internal sealed class ListInstagramAccountsQueryHandler(IKromicFlowDbContext db) : IRequestHandler<ListInstagramAccountsQuery, IReadOnlyList<InstagramAccountDto>>
{
    public async Task<IReadOnlyList<InstagramAccountDto>> Handle(ListInstagramAccountsQuery request, CancellationToken cancellationToken) =>
        await db.InstagramAccounts.Where(x => x.UserId == request.UserId)
            .OrderByDescending(x => x.ConnectedUtc)
            .Select(x => new InstagramAccountDto(x.Id, x.InstagramUserId, x.Username, x.RefreshRequired, x.ConnectedUtc, x.LastSyncUtc))
            .ToListAsync(cancellationToken);
}
