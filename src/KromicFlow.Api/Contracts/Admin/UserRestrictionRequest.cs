namespace KromicFlow.Api.Contracts.Admin;

public sealed record UserRestrictionRequest(bool LoginBlocked, bool AutomationBlocked, bool NotificationBlocked, string? Reason);
