namespace KromicFlow.Application.DTOs.Admin;

public sealed record AdminUserListItemDto(Guid Id, string Email, string FullName, bool IsActive, string PlanCode, DateTime CreatedUtc, bool LoginBlocked, bool AutomationBlocked, bool NotificationBlocked);
