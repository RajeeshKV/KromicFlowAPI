using KromicFlow.Application.Abstractions;
using KromicFlow.Application.Common;
using KromicFlow.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace KromicFlow.Application.Features.Admin.Restrictions;

internal sealed class SetUserRestrictionCommandHandler(IKromicFlowDbContext db, IAuditWriter auditWriter) : IRequestHandler<SetUserRestrictionCommand, Result>
{
    public async Task<Result> Handle(SetUserRestrictionCommand request, CancellationToken cancellationToken)
    {
        var user = await db.Users.FindAsync([request.UserId], cancellationToken);
        if (user is null) return Result.Failure("User not found.");
        var restriction = await db.UserRestrictions.FirstOrDefaultAsync(x => x.UserId == request.UserId, cancellationToken);
        if (restriction is null)
        {
            restriction = new UserRestriction { UserId = request.UserId };
            db.UserRestrictions.Add(restriction);
        }
        restriction.LoginBlocked = request.LoginBlocked;
        restriction.AutomationBlocked = request.AutomationBlocked;
        restriction.NotificationBlocked = request.NotificationBlocked;
        restriction.Reason = request.Reason;
        restriction.SetByAdminId = request.AdminId;
        restriction.UpdatedUtc = DateTime.UtcNow;
        await db.SaveChangesAsync(cancellationToken);
        await auditWriter.WriteAsync("UserRestrictionUpdated", nameof(UserRestriction), restriction.Id.ToString(), null, request.AdminId, null, cancellationToken);
        return Result.Success();
    }
}
