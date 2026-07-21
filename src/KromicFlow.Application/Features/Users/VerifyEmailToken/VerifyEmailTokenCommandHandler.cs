using KromicFlow.Application.Abstractions;
using KromicFlow.Application.Common;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace KromicFlow.Application.Features.Users.VerifyEmailToken;

internal sealed class VerifyEmailTokenCommandHandler(
    IKromicFlowDbContext db,
    IAuditWriter auditWriter,
    ILogger<VerifyEmailTokenCommandHandler> logger) : IRequestHandler<VerifyEmailTokenCommand, Result>
{
    public async Task<Result> Handle(VerifyEmailTokenCommand request, CancellationToken cancellationToken)
    {
        logger.LogInformation("Verifying email token for user {UserId}", request.UserId);

        var user = await db.Users.FirstOrDefaultAsync(x => x.Id == request.UserId, cancellationToken);
        if (user is null)
        {
            logger.LogWarning("User not found: {UserId}", request.UserId);
            return Result.Failure("User not found");
        }

        // Check if already verified
        if (user.EmailVerified)
        {
            logger.LogInformation("User {UserId} already has verified email", request.UserId);
            return Result.Failure("Email is already verified");
        }

        // Check if token exists
        if (string.IsNullOrEmpty(user.EmailVerificationToken))
        {
            logger.LogWarning("No verification token found for user {UserId}", request.UserId);
            return Result.Failure("No verification token found. Please request a new verification email");
        }

        // Validate token
        if (user.EmailVerificationToken != request.Token)
        {
            logger.LogWarning("Invalid verification token for user {UserId}", request.UserId);
            return Result.Failure("Invalid verification token");
        }

        // Check if token expired
        if (user.EmailVerificationTokenExpiresUtc.HasValue && user.EmailVerificationTokenExpiresUtc.Value < DateTime.UtcNow)
        {
            logger.LogWarning("Verification token expired for user {UserId}", request.UserId);
            user.EmailVerificationToken = null;
            user.EmailVerificationTokenExpiresUtc = null;
            await db.SaveChangesAsync(cancellationToken);
            return Result.Failure("Verification token has expired. Please request a new verification email");
        }

        // Mark email as verified
        user.EmailVerified = true;
        user.EmailVerificationToken = null; // Clear token after use
        user.EmailVerificationTokenExpiresUtc = null;
        user.UpdatedUtc = DateTime.UtcNow;

        await db.SaveChangesAsync(cancellationToken);
        await auditWriter.WriteAsync("EmailVerified", nameof(Domain.Entities.User), user.Id.ToString(), user.Id, null, null, cancellationToken);

        logger.LogInformation("Email verified successfully for user {UserId}", request.UserId);
        return Result.Success();
    }
}
