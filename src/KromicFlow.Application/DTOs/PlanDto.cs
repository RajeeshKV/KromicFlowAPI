namespace KromicFlow.Application.DTOs.Admin;

public sealed record PlanDto(
    Guid Id,
    string Code,
    string Name,
    bool IsActive,
    bool IsDefault,
    int MaxInstagramAccounts,
    int MaxAutomations,
    int MonthlyAutomationRuns,
    int MonthlyEmails,
    int MonthlyPushNotifications,
    int PriceInrPaise,
    string BillingPeriod,
    string? RazorpayPlanId
);
