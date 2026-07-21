namespace KromicFlow.Application.DTOs.Auth;

public sealed record UserProfileDto(
    Guid Id,
    string Email,
    string FullName,
    string Role,
    string PlanCode,
    bool IsActive,
    bool EmailVerified,
    bool MarketingEmailEnabled,
    bool MarketingPushEnabled);
