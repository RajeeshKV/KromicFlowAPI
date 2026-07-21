namespace KromicFlow.Application.Options;

/// <summary>
/// Email template configuration
/// </summary>
public sealed class EmailTemplateOptions
{
    /// <summary>
    /// Brevo transactional email template ID for email verification
    /// </summary>
    public int VerificationEmailTemplateId { get; set; }

    /// <summary>
    /// Brevo transactional email template ID for subscription expiry reminder (7 days before)
    /// </summary>
    public int SubscriptionExpiry7DaysTemplateId { get; set; }

    /// <summary>
    /// Brevo transactional email template ID for subscription expiry reminder (3 days before)
    /// </summary>
    public int SubscriptionExpiry3DaysTemplateId { get; set; }

    /// <summary>
    /// Brevo transactional email template ID for subscription expiry reminder (1 day before)
    /// </summary>
    public int SubscriptionExpiry1DayTemplateId { get; set; }

    /// <summary>
    /// Brevo transactional email template ID for subscription expiry notification (on expiry date)
    /// </summary>
    public int SubscriptionExpiredTemplateId { get; set; }

    /// <summary>
    /// Brevo transactional email template ID for subscription renewal confirmation
    /// </summary>
    public int SubscriptionRenewalTemplateId { get; set; }

    /// <summary>
    /// Default: false. If false, templates are disabled and emails are sent with inline HTML.
    /// If true, Brevo template IDs above must be set and emails will use template rendering.
    /// </summary>
    public bool UseTemplates { get; set; } = false;
}
