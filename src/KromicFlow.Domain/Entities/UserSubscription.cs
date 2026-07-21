namespace KromicFlow.Domain.Entities;

/// <summary>
/// Tracks a user's Razorpay subscription lifecycle.
/// One active subscription per user at any time.
/// Historical subscriptions are retained with status Cancelled/Completed/Expired.
/// </summary>
public sealed class UserSubscription : Entity
{
    public Guid UserId { get; set; }
    public User User { get; set; } = null!;

    public Guid PlanId { get; set; }
    public Plan Plan { get; set; } = null!;

    // Razorpay identifiers
    public string RazorpaySubscriptionId { get; set; } = string.Empty;  // sub_xxxx
    public string RazorpayPlanId { get; set; } = string.Empty;          // plan_xxxx
    public string? RazorpayCustomerId { get; set; }                     // cust_xxxx (populated after auth payment)
    public string? RazorpayPaymentId { get; set; }                      // pay_xxxx (latest payment)

    // Subscription status — mirrors Razorpay status values
    // created | authenticated | active | pending | halted | cancelled | completed | expired
    public string Status { get; set; } = "created";

    // Billing period
    public DateTime? CurrentPeriodStart { get; set; }
    public DateTime? CurrentPeriodEnd { get; set; }
    public DateTime? CancelledAtUtc { get; set; }
    public DateTime? ActivatedAtUtc { get; set; }

    // Billing counters (synced from Razorpay)
    public int PaidCount { get; set; } = 0;
    public int TotalCount { get; set; } = 0;

    // Full JSON of the latest Razorpay subscription object — stored for debugging/audit
    public string? RazorpaySnapshotJson { get; set; }
}
