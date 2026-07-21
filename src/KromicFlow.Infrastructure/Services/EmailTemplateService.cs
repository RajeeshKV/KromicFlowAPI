using KromicFlow.Application.Abstractions;
using KromicFlow.Application.Options;
using Microsoft.Extensions.Options;

namespace KromicFlow.Infrastructure.Services;

/// <summary>
/// Implementation of email template service
/// </summary>
public sealed class EmailTemplateService(IOptions<EmailTemplateOptions> options) : IEmailTemplateService
{
    private readonly EmailTemplateOptions _options = options.Value;

    public int? GetTemplateId(EmailTemplateType templateType)
    {
        if (!_options.UseTemplates)
            return null;

        return templateType switch
        {
            EmailTemplateType.VerificationEmail => _options.VerificationEmailTemplateId,
            EmailTemplateType.SubscriptionExpiry7Days => _options.SubscriptionExpiry7DaysTemplateId,
            EmailTemplateType.SubscriptionExpiry3Days => _options.SubscriptionExpiry3DaysTemplateId,
            EmailTemplateType.SubscriptionExpiry1Day => _options.SubscriptionExpiry1DayTemplateId,
            EmailTemplateType.SubscriptionExpired => _options.SubscriptionExpiredTemplateId,
            EmailTemplateType.SubscriptionRenewal => _options.SubscriptionRenewalTemplateId,
            _ => null
        };
    }

    public string RenderSubject(EmailTemplateType templateType, Dictionary<string, string> parameters)
    {
        return templateType switch
        {
            EmailTemplateType.VerificationEmail => "Verify your KromicFlow email",
            EmailTemplateType.SubscriptionExpiry7Days => "Your KromicFlow subscription expires in 7 days",
            EmailTemplateType.SubscriptionExpiry3Days => "Your KromicFlow subscription expires in 3 days",
            EmailTemplateType.SubscriptionExpiry1Day => "Your KromicFlow subscription expires tomorrow",
            EmailTemplateType.SubscriptionExpired => "Your KromicFlow subscription has expired",
            EmailTemplateType.SubscriptionRenewal => "Your KromicFlow subscription has been renewed",
            _ => "KromicFlow Notification"
        };
    }

    public string RenderBody(EmailTemplateType templateType, Dictionary<string, string> parameters)
    {
        return templateType switch
        {
            EmailTemplateType.VerificationEmail => RenderVerificationEmail(parameters),
            EmailTemplateType.SubscriptionExpiry7Days => RenderSubscriptionExpiry7Days(parameters),
            EmailTemplateType.SubscriptionExpiry3Days => RenderSubscriptionExpiry3Days(parameters),
            EmailTemplateType.SubscriptionExpiry1Day => RenderSubscriptionExpiry1Day(parameters),
            EmailTemplateType.SubscriptionExpired => RenderSubscriptionExpired(parameters),
            EmailTemplateType.SubscriptionRenewal => RenderSubscriptionRenewal(parameters),
            _ => "<p>No template found</p>"
        };
    }

    public bool AreTemplatesEnabled() => _options.UseTemplates;

    #region Email Templates

    private static string RenderVerificationEmail(Dictionary<string, string> parameters)
    {
        var fullName = parameters.TryGetValue("fullName", out var name) ? name : "User";
        var verificationLink = parameters.TryGetValue("verificationLink", out var link) ? link : "#";

        return $@"
<h2>Verify Your Email</h2>
<p>Hi {fullName},</p>
<p>Thank you for signing up for KromicFlow!</p>
<p>To activate your automations, please verify your email by clicking the link below:</p>
<p><a href=""{verificationLink}"" style=""background-color: #3b82f6; color: white; padding: 10px 20px; text-decoration: none; border-radius: 4px; display: inline-block;"">Verify Email</a></p>
<p>Or copy and paste this link: {verificationLink}</p>
<p>This link expires in 24 hours.</p>
<p>If you didn't sign up, you can ignore this email.</p>
<p>Best regards,<br>KromicFlow Team</p>";
    }

    private static string RenderSubscriptionExpiry7Days(Dictionary<string, string> parameters)
    {
        var fullName = parameters.TryGetValue("fullName", out var name) ? name : "User";
        var planName = parameters.TryGetValue("planName", out var plan) ? plan : "your plan";
        var expiryDate = parameters.TryGetValue("expiryDate", out var date) ? date : "soon";
        var renewalLink = parameters.TryGetValue("renewalLink", out var link) ? link : "#";

        return $@"
<h2>Your Subscription Expires Soon</h2>
<p>Hi {fullName},</p>
<p>Your {planName} subscription will expire on {expiryDate} (in 7 days).</p>
<p>To continue using KromicFlow and keep your automations running, please renew your subscription:</p>
<p><a href=""{renewalLink}"" style=""background-color: #10b981; color: white; padding: 10px 20px; text-decoration: none; border-radius: 4px; display: inline-block;"">Renew Subscription</a></p>
<p>Without renewal, your automations will be disabled.</p>
<p>Best regards,<br>KromicFlow Team</p>";
    }

    private static string RenderSubscriptionExpiry3Days(Dictionary<string, string> parameters)
    {
        var fullName = parameters.TryGetValue("fullName", out var name) ? name : "User";
        var planName = parameters.TryGetValue("planName", out var plan) ? plan : "your plan";
        var expiryDate = parameters.TryGetValue("expiryDate", out var date) ? date : "soon";
        var renewalLink = parameters.TryGetValue("renewalLink", out var link) ? link : "#";

        return $@"
<h2>Your Subscription Expires in 3 Days</h2>
<p>Hi {fullName},</p>
<p><strong>Urgent:</strong> Your {planName} subscription will expire on {expiryDate} (in 3 days).</p>
<p>Please renew your subscription to avoid losing access to your automations:</p>
<p><a href=""{renewalLink}"" style=""background-color: #f59e0b; color: white; padding: 10px 20px; text-decoration: none; border-radius: 4px; display: inline-block;"">Renew Now</a></p>
<p>Your automations will be automatically disabled if your subscription expires.</p>
<p>Best regards,<br>KromicFlow Team</p>";
    }

    private static string RenderSubscriptionExpiry1Day(Dictionary<string, string> parameters)
    {
        var fullName = parameters.TryGetValue("fullName", out var name) ? name : "User";
        var planName = parameters.TryGetValue("planName", out var plan) ? plan : "your plan";
        var expiryDate = parameters.TryGetValue("expiryDate", out var date) ? date : "tomorrow";
        var renewalLink = parameters.TryGetValue("renewalLink", out var link) ? link : "#";

        return $@"
<h2>Your Subscription Expires Tomorrow</h2>
<p>Hi {fullName},</p>
<p><strong style=""color: #dc2626;"">URGENT:</strong> Your {planName} subscription will expire on {expiryDate} (tomorrow).</p>
<p>Renew your subscription immediately to keep your automations active:</p>
<p><a href=""{renewalLink}"" style=""background-color: #dc2626; color: white; padding: 10px 20px; text-decoration: none; border-radius: 4px; display: inline-block;"">Renew Immediately</a></p>
<p>All automations will be disabled starting tomorrow.</p>
<p>Best regards,<br>KromicFlow Team</p>";
    }

    private static string RenderSubscriptionExpired(Dictionary<string, string> parameters)
    {
        var fullName = parameters.TryGetValue("fullName", out var name) ? name : "User";
        var planName = parameters.TryGetValue("planName", out var plan) ? plan : "your plan";
        var renewalLink = parameters.TryGetValue("renewalLink", out var link) ? link : "#";

        return $@"
<h2>Your Subscription Has Expired</h2>
<p>Hi {fullName},</p>
<p>Your {planName} subscription has expired.</p>
<p><strong>Your automations are now disabled.</strong> To resume your automations and continue using KromicFlow, please renew your subscription:</p>
<p><a href=""{renewalLink}"" style=""background-color: #dc2626; color: white; padding: 10px 20px; text-decoration: none; border-radius: 4px; display: inline-block;"">Renew Subscription</a></p>
<p>Don't lose your automation setup. Renew now to get back online.</p>
<p>Best regards,<br>KromicFlow Team</p>";
    }

    private static string RenderSubscriptionRenewal(Dictionary<string, string> parameters)
    {
        var fullName = parameters.TryGetValue("fullName", out var name) ? name : "User";
        var planName = parameters.TryGetValue("planName", out var plan) ? plan : "your plan";
        var newExpiryDate = parameters.TryGetValue("newExpiryDate", out var date) ? date : "next month";
        var amount = parameters.TryGetValue("amount", out var amt) ? amt : "your subscription";
        var transactionId = parameters.TryGetValue("transactionId", out var txn) ? txn : "";

        var txnInfo = !string.IsNullOrEmpty(transactionId) ? $"<p><strong>Transaction ID:</strong> {transactionId}</p>" : "";

        return $@"
<h2>Subscription Renewed Successfully</h2>
<p>Hi {fullName},</p>
<p>Thank you for renewing your {planName} subscription!</p>
<p><strong>Renewal Details:</strong></p>
<ul>
  <li>Plan: {planName}</li>
  <li>Amount: {amount}</li>
  <li>New Expiry Date: {newExpiryDate}</li>
</ul>
{txnInfo}
<p>Your automations are now active and will continue running without interruption.</p>
<p>If you have any questions or need assistance, feel free to reach out to us.</p>
<p>Best regards,<br>KromicFlow Team</p>";
    }

    #endregion
}
