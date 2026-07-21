namespace KromicFlow.Application.Abstractions;

/// <summary>
/// Enum for email template types
/// </summary>
public enum EmailTemplateType
{
    /// <summary>
    /// Email verification link
    /// </summary>
    VerificationEmail = 1,

    /// <summary>
    /// Subscription expiry reminder - 7 days before expiry
    /// </summary>
    SubscriptionExpiry7Days = 2,

    /// <summary>
    /// Subscription expiry reminder - 3 days before expiry
    /// </summary>
    SubscriptionExpiry3Days = 3,

    /// <summary>
    /// Subscription expiry reminder - 1 day before expiry
    /// </summary>
    SubscriptionExpiry1Day = 4,

    /// <summary>
    /// Subscription expired notification - on expiry date
    /// </summary>
    SubscriptionExpired = 5,

    /// <summary>
    /// Subscription renewal confirmation
    /// </summary>
    SubscriptionRenewal = 6
}
