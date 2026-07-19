using KromicFlow.Application.Abstractions;
using KromicFlow.Application.Common;
using KromicFlow.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace KromicFlow.Application.Services;

public sealed class AutomationEligibilityService(IKromicFlowDbContext db) : IAutomationEligibilityService
{
    public async Task<Result> ValidateAccountEligibilityAsync(Guid userId, Guid instagramAccountId, CancellationToken cancellationToken = default)
    {
        // Check if account exists
        var account = await db.InstagramAccounts
            .Include(x => x.User)
            .FirstOrDefaultAsync(x => x.Id == instagramAccountId, cancellationToken);

        if (account == null)
        {
            return Result.Failure("Instagram account not found.");
        }

        // Check if account belongs to the user
        if (account.UserId != userId)
        {
            return Result.Failure("Account does not belong to the current user.");
        }

        // Check if account is connected
        if (!account.IsConnected)
        {
            return Result.Failure("Account is not connected. Please connect the account to enable automations.");
        }

        // Check if user is active
        if (!account.User.IsActive)
        {
            return Result.Failure("User account is not active.");
        }

        // Check if token is valid
        if (account.TokenExpiresUtc.HasValue && account.TokenExpiresUtc.Value < DateTime.UtcNow)
        {
            return Result.Failure("Account access token has expired. Please re-authenticate.");
        }

        // Check if token status is valid
        if (account.TokenStatus != "active")
        {
            return Result.Failure($"Account token status is {account.TokenStatus}. Please re-authenticate.");
        }

        // Check if user has login restrictions
        var loginBlocked = await db.UserRestrictions.AnyAsync(x => x.UserId == userId && x.LoginBlocked, cancellationToken);
        if (loginBlocked)
        {
            return Result.Failure("User account has login restrictions.");
        }

        return Result.Success();
    }
}
