namespace KromicFlow.Api.Contracts.Admin;

public sealed record PlanRequest(string Code, string Name, bool IsActive, bool IsDefault, int MaxInstagramAccounts, int MaxAutomations, int MonthlyAutomationRuns, int MonthlyEmails, int MonthlyPushNotifications);
