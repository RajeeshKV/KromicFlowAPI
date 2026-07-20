namespace KromicFlow.Domain.Entities;

public sealed class Plan : Entity
{
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
    public bool IsDefault { get; set; }
    public int MaxInstagramAccounts { get; set; } = 1;
    public int MaxAutomations { get; set; } = 3;
    public int MonthlyAutomationRuns { get; set; } = 100;
    public int MonthlyEmails { get; set; } = 25;
    public int MonthlyPushNotifications { get; set; } = 25;
    public string ConfigurationJson { get; set; } = "{}";

    // Pricing — stored in INR paise (100 paise = ₹1) to avoid decimals
    // 0 = free plan
    public int PriceInrPaise { get; set; } = 0;
    public string BillingPeriod { get; set; } = "monthly"; // monthly, yearly, lifetime
}
