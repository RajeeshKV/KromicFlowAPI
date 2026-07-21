using KromicFlow.Application.Abstractions;
using KromicFlow.Application.Common;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace KromicFlow.Application.Features.Users.SendVerificationEmail;

internal sealed class SendVerificationEmailCommandHandler(
    IKromicFlowDbContext db,
    IEmailVerificationService emailVerificationService,
    INotificationSender notificationSender,
    IAuditWriter auditWriter,
    ILogger<SendVerificationEmailCommandHandler> logger) : IRequestHandler<SendVerificationEmailCommand, Result>
{
    public async Task<Result> Handle(SendVerificationEmailCommand request, CancellationToken cancellationToken)
    {
        logger.LogInformation("Sending verification email to {Email} for user {UserId}", request.Email, request.UserId);

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

        // Check rate limiting: max 3 verification emails per hour
        var recentVerificationEmails = await db.NotificationMessages
            .Where(x => x.UserId == request.UserId && 
                       x.Subject.Contains("verify") &&
                       x.CreatedUtc > DateTime.UtcNow.AddHours(-1))
            .CountAsync(cancellationToken);

        if (recentVerificationEmails >= 3)
        {
            logger.LogWarning("Rate limit exceeded for user {UserId}: {Count} emails in last hour", request.UserId, recentVerificationEmails);
            return Result.Failure("Too many verification requests. Please try again in 1 hour");
        }

        // Generate verification token
        var token = emailVerificationService.GenerateToken();
        var tokenExpiry = emailVerificationService.GetTokenExpirationTime();

        // Update user with token
        user.EmailVerificationToken = token;
        user.EmailVerificationTokenExpiresUtc = tokenExpiry;
        user.Email = request.Email; // Store the email provided by user

        // Create verification link (frontend will use this)
        var verificationLink = $"https://yourdomain.com/verify-email?token={Uri.EscapeDataString(token)}";

        // Send email via Brevo
        var emailBody = $@"
<h2>Verify Your Email</h2>
<p>Hi {user.FullName},</p>
<p>Thank you for signing up for KromicFlow!</p>
<p>To activate your automations, please verify your email by clicking the link below:</p>
<p><a href=""{verificationLink}"" style=""background-color: #3b82f6; color: white; padding: 10px 20px; text-decoration: none; border-radius: 4px; display: inline-block;"">Verify Email</a></p>
<p>Or copy and paste this link: {verificationLink}</p>
<p>This link expires in 24 hours.</p>
<p>If you didn't sign up, you can ignore this email.</p>
<p>Best regards,<br>KromicFlow Team</p>";

        try
        {
            var messageId = await notificationSender.SendEmailAsync(
                request.Email,
                "Verify your KromicFlow email",
                emailBody,
                cancellationToken);

            logger.LogInformation("Verification email sent successfully to {Email}. MessageId: {MessageId}", request.Email, messageId);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to send verification email to {Email}", request.Email);
            return Result.Failure("Failed to send verification email. Please try again");
        }

        // Save changes
        await db.SaveChangesAsync(cancellationToken);
        await auditWriter.WriteAsync("VerificationEmailSent", nameof(Domain.Entities.User), user.Id.ToString(), user.Id, null, null, cancellationToken);

        logger.LogInformation("Verification email sent successfully for user {UserId}", request.UserId);
        return Result.Success();
    }
}
