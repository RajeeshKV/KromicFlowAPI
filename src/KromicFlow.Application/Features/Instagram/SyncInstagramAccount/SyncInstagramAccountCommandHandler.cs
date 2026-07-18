using KromicFlow.Application.Abstractions;
using KromicFlow.Application.Common;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace KromicFlow.Application.Features.Instagram.SyncInstagramAccount;

internal sealed class SyncInstagramAccountCommandHandler(IKromicFlowDbContext db, IMetaApiClient metaApiClient) : IRequestHandler<SyncInstagramAccountCommand, Result>
{
    public async Task<Result> Handle(SyncInstagramAccountCommand request, CancellationToken cancellationToken)
    {
        var account = await db.InstagramAccounts.FirstOrDefaultAsync(x => x.Id == request.InstagramAccountId && x.UserId == request.UserId, cancellationToken);
        if (account is null) return Result.Failure("Instagram account not found.");
        await metaApiClient.SyncMediaAsync(account, cancellationToken);
        await db.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}
