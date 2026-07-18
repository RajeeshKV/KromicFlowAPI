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
}
