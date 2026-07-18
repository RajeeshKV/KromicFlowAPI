using KromicFlow.Application.Abstractions;
using KromicFlow.Application.Common;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace KromicFlow.Application.Features.Auth.Logout;

internal sealed class LogoutAllCommandHandler(IKromicFlowDbContext db) : IRequestHandler<LogoutAllCommand, Result>
{
    public async Task<Result> Handle(LogoutAllCommand request, CancellationToken cancellationToken)
    {
        if (request.IsAdmin)
        {
            var admin = await db.AdminUsers.FindAsync([request.ActorId], cancellationToken);
            if (admin is null) return Result.Failure("Admin not found.");
            admin.TokenVersion++;
            var sessions = await db.Sessions.Where(x => x.AdminUserId == request.ActorId && x.RevokedUtc == null).ToListAsync(cancellationToken);
            foreach (var session in sessions) session.RevokedUtc = DateTime.UtcNow;
        }
        else
        {
            var user = await db.Users.FindAsync([request.ActorId], cancellationToken);
            if (user is null) return Result.Failure("User not found.");
            user.TokenVersion++;
            var sessions = await db.Sessions.Where(x => x.UserId == request.ActorId && x.RevokedUtc == null).ToListAsync(cancellationToken);
            foreach (var session in sessions) session.RevokedUtc = DateTime.UtcNow;
        }

        await db.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}
