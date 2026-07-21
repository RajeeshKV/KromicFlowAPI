namespace KromicFlow.Application.Features.Admin.Restrictions;

public sealed record UserRestrictionDto(Guid Id, Guid UserId, bool LoginBlocked, bool AutomationBlocked, bool NotificationBlocked, string? Reason, Guid? SetByAdminId, DateTime UpdatedUtc);

