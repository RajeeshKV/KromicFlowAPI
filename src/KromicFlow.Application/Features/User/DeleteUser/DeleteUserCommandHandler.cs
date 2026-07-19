using KromicFlow.Application.Abstractions;
using KromicFlow.Application.Common;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace KromicFlow.Application.Features.User.DeleteUser;

internal sealed class DeleteUserCommandHandler(
    IKromicFlowDbContext db,
    ILogger<DeleteUserCommandHandler> logger) : IRequestHandler<DeleteUserCommand, Result>
{
    public async Task<Result> Handle(DeleteUserCommand request, CancellationToken cancellationToken)
    {
        logger.LogInformation("Starting user account deletion");

        // Get user with all related data
        var user = await db.Users
            .Include(x => x.InstagramAccounts)
            .Include(x => x.Sessions)
            .Include(x => x.Automations)
            .FirstOrDefaultAsync(cancellationToken);

        if (user is null)
        {
            logger.LogError("User not found");
            return Result.Failure("User not found.");
        }

        // Delete related data (cascading)
        logger.LogInformation("Deleting {AutomationCount} automations", user.Automations.Count);
        db.Automations.RemoveRange(user.Automations);

        logger.LogInformation("Deleting {SessionCount} sessions", user.Sessions.Count);
        db.Sessions.RemoveRange(user.Sessions);

        logger.LogInformation("Deleting {InstagramAccountCount} Instagram accounts", user.InstagramAccounts.Count);
        db.InstagramAccounts.RemoveRange(user.InstagramAccounts);

        // Delete user
        db.Users.Remove(user);

        await db.SaveChangesAsync(cancellationToken);

        logger.LogInformation("User account deleted successfully");
        return Result.Success();
    }
}
