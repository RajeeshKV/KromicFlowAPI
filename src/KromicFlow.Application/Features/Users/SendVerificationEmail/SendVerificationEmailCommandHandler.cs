using KromicFlow.Application.Abstractions;
using KromicFlow.Application.Common;
using KromicFlow.Application.Options;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace KromicFlow.Application.Features.Users.SendVerificationEmail;

internal sealed class SendVerificationEmailCommandHandler(
    IKromicFlowDbContext db,
    IEmailVerificationService emailVerificationService,
    IEmailTemplateService emailTemplateService,
    INotificationSender notificationSender,
    IAuditWriter auditWriter,
    IOptions<PlatformOptions> platformOptions,
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
        var verificationLink = $"{platformOptions.Value.EmailVerificationRedirectUrl}?token={Uri.EscapeDataString(token)}";

        // Prepare email template parameters
        var templateParams = new Dictionary<string, string>
        {
            { "fullName", user.FullName },
            { "verificationLink", verificationLink }
        };

        // Render email subject and body
        var subject = emailTemplateService.RenderSubject(EmailTemplateType.VerificationEmail, templateParams);
        var emailBody = emailTemplateService.RenderBody(EmailTemplateType.VerificationEmail, templateParams);

        try
        {
            var messageId = await notificationSender.SendEmailAsync(
                request.Email,
                subject,
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
