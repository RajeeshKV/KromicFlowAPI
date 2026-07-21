# Email Template System - Code Examples

Examples showing how to use the email template system in handlers and services.

---

## Example 1: Send Verification Email

**File**: `SendVerificationEmailCommandHandler.cs`

```csharp
using KromicFlow.Application.Abstractions;
using KromicFlow.Application.Common;
using MediatR;

public class SendVerificationEmailCommandHandler(
    IKromicFlowDbContext db,
    IEmailVerificationService emailVerificationService,
    IEmailTemplateService emailTemplateService,
    INotificationSender notificationSender,
    ILogger<SendVerificationEmailCommandHandler> logger) 
    : IRequestHandler<SendVerificationEmailCommand, Result>
{
    public async Task<Result> Handle(SendVerificationEmailCommand request, CancellationToken cancellationToken)
    {
        var user = await db.Users.FirstOrDefaultAsync(x => x.Id == request.UserId, cancellationToken);
        if (user is null) return Result.Failure("User not found");

        // Generate verification token
        var token = emailVerificationService.GenerateToken();
        var tokenExpiry = emailVerificationService.GetTokenExpirationTime();
        
        user.EmailVerificationToken = token;
        user.EmailVerificationTokenExpiresUtc = tokenExpiry;
        user.Email = request.Email;

        // Create verification link
        var verificationLink = $"https://yourdomain.com/verify-email?token={Uri.EscapeDataString(token)}";

        // Prepare template parameters
        var templateParams = new Dictionary<string, string>
        {
            { "fullName", user.FullName },
            { "verificationLink", verificationLink }
        };

        // Render email using template service
        var subject = emailTemplateService.RenderSubject(EmailTemplateType.VerificationEmail, templateParams);
        var emailBody = emailTemplateService.RenderBody(EmailTemplateType.VerificationEmail, templateParams);

        // Send email
        await notificationSender.SendEmailAsync(
            request.Email,
            subject,
            emailBody,
            cancellationToken);

        await db.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}
```

---

## Example 2: Send Subscription Expiry Reminder (7 Days)

**File**: `SubscriptionExpiryReminderBackgroundService.cs`

```csharp
using KromicFlow.Application.Abstractions;

public class SubscriptionExpiryReminderBackgroundService(
    IServiceProvider serviceProvider,
    ILogger<SubscriptionExpiryReminderBackgroundService> logger)
    : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = serviceProvider.CreateScope();
                var db = scope.ServiceProvider.GetRequiredService<IKromicFlowDbContext>();
                var emailTemplateService = scope.ServiceProvider.GetRequiredService<IEmailTemplateService>();
                var notificationSender = scope.ServiceProvider.GetRequiredService<INotificationSender>();

                // Find subscriptions expiring in 7 days
                var sevenDaysLater = DateTime.UtcNow.AddDays(7).Date;
                var expiringSubscriptions = await db.Subscriptions
                    .Include(x => x.User)
                    .Where(x => x.ExpiryDateUtc.Date == sevenDaysLater)
                    .ToListAsync(stoppingToken);

                foreach (var subscription in expiringSubscriptions)
                {
                    var templateParams = new Dictionary<string, string>
                    {
                        { "fullName", subscription.User.FullName },
                        { "planName", subscription.PlanCode },
                        { "expiryDate", subscription.ExpiryDateUtc:yyyy-MM-dd },
                        { "renewalLink", "https://yourdomain.com/billing/renew" }
                    };

                    var subject = emailTemplateService.RenderSubject(
                        EmailTemplateType.SubscriptionExpiry7Days, 
                        templateParams);
                    
                    var body = emailTemplateService.RenderBody(
                        EmailTemplateType.SubscriptionExpiry7Days, 
                        templateParams);

                    await notificationSender.SendEmailAsync(
                        subscription.User.Email,
                        subject,
                        body,
                        stoppingToken);

                    logger.LogInformation(
                        "Sent 7-day expiry reminder to {Email}", 
                        subscription.User.Email);
                }

                // Run every 6 hours
                await Task.Delay(TimeSpan.FromHours(6), stoppingToken);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error in subscription expiry reminder service");
                await Task.Delay(TimeSpan.FromMinutes(5), stoppingToken);
            }
        }
    }
}
```

---

## Example 3: Send Subscription Renewal Confirmation

```csharp
public class ProcessSubscriptionRenewalCommand : IRequestHandler<ProcessSubscriptionRenewalCommand, Result>
{
    public async Task<Result> Handle(
        ProcessSubscriptionRenewalCommand request, 
        CancellationToken cancellationToken)
    {
        var subscription = await db.Subscriptions
            .Include(x => x.User)
            .FirstOrDefaultAsync(x => x.Id == request.SubscriptionId, cancellationToken);

        if (subscription is null) return Result.Failure("Subscription not found");

        // Update subscription with new expiry date
        subscription.ExpiryDateUtc = DateTime.UtcNow.AddMonths(1);
        subscription.RenewedUtc = DateTime.UtcNow;

        // Prepare renewal confirmation email parameters
        var templateParams = new Dictionary<string, string>
        {
            { "fullName", subscription.User.FullName },
            { "planName", subscription.PlanCode },
            { "newExpiryDate", subscription.ExpiryDateUtc.ToString("yyyy-MM-dd") },
            { "amount", request.Amount }, // e.g., "₹999"
            { "transactionId", request.RazorpayTransactionId }
        };

        // Render and send confirmation email
        var subject = emailTemplateService.RenderSubject(
            EmailTemplateType.SubscriptionRenewal, 
            templateParams);
        
        var body = emailTemplateService.RenderBody(
            EmailTemplateType.SubscriptionRenewal, 
            templateParams);

        await notificationSender.SendEmailAsync(
            subscription.User.Email,
            subject,
            body,
            cancellationToken);

        await db.SaveChangesAsync(cancellationToken);
        logger.LogInformation("Sent renewal confirmation to {Email}", subscription.User.Email);
        
        return Result.Success();
    }
}
```

---

## Example 4: Conditional Template Usage

Show how to handle both inline and template-based rendering:

```csharp
public class SendEmailService(
    IEmailTemplateService emailTemplateService,
    INotificationSender notificationSender)
{
    public async Task SendEmailAsync(
        string email,
        EmailTemplateType templateType,
        Dictionary<string, string> parameters,
        CancellationToken cancellationToken)
    {
        // Get subject and body using template service
        var subject = emailTemplateService.RenderSubject(templateType, parameters);
        var body = emailTemplateService.RenderBody(templateType, parameters);

        // Check if templates are enabled
        if (emailTemplateService.AreTemplatesEnabled())
        {
            // Use Brevo template ID for rendering
            var templateId = emailTemplateService.GetTemplateId(templateType);
            if (templateId.HasValue)
            {
                // Send using Brevo template (TODO: implement in BrevoNotificationSender)
                // await notificationSender.SendEmailWithTemplateAsync(
                //     email, 
                //     templateId.Value, 
                //     parameters, 
                //     cancellationToken);
                return;
            }
        }

        // Fall back to inline HTML rendering
        await notificationSender.SendEmailAsync(email, subject, body, cancellationToken);
    }
}
```

---

## Example 5: Testing Template Rendering

```csharp
[TestFixture]
public class EmailTemplateServiceTests
{
    private IEmailTemplateService _emailTemplateService;

    [SetUp]
    public void Setup()
    {
        var options = Options.Create(new EmailTemplateOptions { UseTemplates = false });
        _emailTemplateService = new EmailTemplateService(options);
    }

    [Test]
    public void TestVerificationEmailTemplate()
    {
        var parameters = new Dictionary<string, string>
        {
            { "fullName", "John Doe" },
            { "verificationLink", "https://example.com/verify?token=abc123" }
        };

        var subject = _emailTemplateService.RenderSubject(
            EmailTemplateType.VerificationEmail, 
            parameters);
        
        var body = _emailTemplateService.RenderBody(
            EmailTemplateType.VerificationEmail, 
            parameters);

        Assert.That(subject, Contains.Substring("Verify"));
        Assert.That(body, Contains.Substring("John Doe"));
        Assert.That(body, Contains.Substring("verify?token=abc123"));
    }

    [Test]
    public void TestSubscriptionExpiryTemplate()
    {
        var parameters = new Dictionary<string, string>
        {
            { "fullName", "Jane Smith" },
            { "planName", "Pro" },
            { "expiryDate", "2026-08-20" },
            { "renewalLink", "https://example.com/renew" }
        };

        var subject = _emailTemplateService.RenderSubject(
            EmailTemplateType.SubscriptionExpiry7Days, 
            parameters);
        
        var body = _emailTemplateService.RenderBody(
            EmailTemplateType.SubscriptionExpiry7Days, 
            parameters);

        Assert.That(subject, Contains.Substring("7 days"));
        Assert.That(body, Contains.Substring("2026-08-20"));
        Assert.That(body, Contains.Substring("Pro"));
    }
}
```

---

## Example 6: Using Template IDs from Configuration

```csharp
public class SubscriptionExpiryService(
    IOptions<EmailTemplateOptions> options,
    IEmailTemplateService emailTemplateService,
    INotificationSender notificationSender)
{
    public async Task SendExpiryReminderAsync(User user, Subscription subscription)
    {
        // Determine template type based on days remaining
        var daysRemaining = (subscription.ExpiryDateUtc - DateTime.UtcNow).Days;
        
        EmailTemplateType templateType = daysRemaining switch
        {
            7 => EmailTemplateType.SubscriptionExpiry7Days,
            3 => EmailTemplateType.SubscriptionExpiry3Days,
            1 => EmailTemplateType.SubscriptionExpiry1Day,
            0 => EmailTemplateType.SubscriptionExpired,
            _ => EmailTemplateType.SubscriptionExpiry7Days
        };

        var templateParams = new Dictionary<string, string>
        {
            { "fullName", user.FullName },
            { "planName", subscription.PlanCode },
            { "expiryDate", subscription.ExpiryDateUtc.ToString("yyyy-MM-dd") },
            { "renewalLink", "https://yourdomain.com/billing/renew" }
        };

        var subject = emailTemplateService.RenderSubject(templateType, templateParams);
        var body = emailTemplateService.RenderBody(templateType, templateParams);

        // If Brevo templates are enabled, use template ID
        if (emailTemplateService.AreTemplatesEnabled())
        {
            var templateId = emailTemplateService.GetTemplateId(templateType);
            if (templateId.HasValue)
            {
                // TODO: Send using template ID
                // This would be implemented in BrevoNotificationSender
                return;
            }
        }

        // Otherwise, send inline HTML
        await notificationSender.SendEmailAsync(user.Email, subject, body);
    }
}
```

---

## Key Methods

### IEmailTemplateService Interface

```csharp
public interface IEmailTemplateService
{
    /// <summary>
    /// Get Brevo template ID for a template type (null if UseTemplates is false)
    /// </summary>
    int? GetTemplateId(EmailTemplateType templateType);

    /// <summary>
    /// Render email subject using template parameters
    /// </summary>
    string RenderSubject(EmailTemplateType templateType, Dictionary<string, string> parameters);

    /// <summary>
    /// Render email HTML body using template parameters
    /// </summary>
    string RenderBody(EmailTemplateType templateType, Dictionary<string, string> parameters);

    /// <summary>
    /// Check if Brevo templates are enabled
    /// </summary>
    bool AreTemplatesEnabled();
}
```

### Usage Pattern

```csharp
// 1. Prepare parameters
var templateParams = new Dictionary<string, string>
{
    { "fullName", user.FullName },
    { "verificationLink", verificationLink }
};

// 2. Render using service
var subject = emailTemplateService.RenderSubject(EmailTemplateType.VerificationEmail, templateParams);
var body = emailTemplateService.RenderBody(EmailTemplateType.VerificationEmail, templateParams);

// 3. Send via INotificationSender
await notificationSender.SendEmailAsync(email, subject, body, cancellationToken);
```

---

## Parameter Guidelines

### For All Templates
- `fullName` - Always required, user's full name
- Dates should be formatted as `yyyy-MM-dd` (ISO format)

### For Expiry Reminders
- `planName` - Plan name (e.g., "Starter", "Pro", "Business")
- `expiryDate` - Subscription expiry date
- `renewalLink` - Link to renewal page (e.g., https://yourdomain.com/billing/renew)

### For Renewal Confirmation
- `newExpiryDate` - Next subscription expiry date
- `amount` - Amount charged (with currency symbol, e.g., "₹999")
- `transactionId` - Razorpay transaction ID (optional but recommended)

---

## Configuration Example

**appsettings.json** (Development - Inline HTML):
```json
{
  "EmailTemplates": {
    "UseTemplates": false
  }
}
```

**appsettings.Production.json** (Production - Brevo Templates):
```json
{
  "EmailTemplates": {
    "UseTemplates": true,
    "VerificationEmailTemplateId": 1,
    "SubscriptionExpiry7DaysTemplateId": 2,
    "SubscriptionExpiry3DaysTemplateId": 3,
    "SubscriptionExpiry1DayTemplateId": 4,
    "SubscriptionExpiredTemplateId": 5,
    "SubscriptionRenewalTemplateId": 6
  }
}
```

---

## Next Steps

1. Create templates in Brevo console (see [TemplateSpecifications.md](./02-TemplateSpecifications.md))
2. Record template IDs
3. Update `appsettings.Production.json` with template IDs
4. Implement `BrevoNotificationSender.SendEmailWithTemplateAsync()` to use template IDs
5. Set `"UseTemplates": true` in production to enable template rendering

