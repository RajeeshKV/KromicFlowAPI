using KromicFlow.Application.Abstractions;
using KromicFlow.Application.Common;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace KromicFlow.Application.Features.Auth.Logout;

internal sealed class LogoutCommandHandler(IKromicFlowDbContext db) : IRequestHandler<LogoutCommand, Result>
{
    public async Task<Result> Handle(LogoutCommand request, CancellationToken cancellationToken)
    {
        var session = await db.Sessions.FirstOrDefaultAsync(x => x.SessionGuid == request.SessionGuid, cancellationToken);
        if (session is not null) session.RevokedUtc = DateTime.UtcNow;
        await db.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}
