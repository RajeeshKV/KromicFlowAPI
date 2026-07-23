using KromicFlow.Domain.Enums;

namespace KromicFlow.Application.DTOs.Automations;

public sealed record AutomationDto(
    Guid Id,
    Guid InstagramAccountId,
    string Name,
    AutomationScope Scope,
    AutomationTriggerType TriggerType,
    string[] Keywords,
    string? PublicReply,
    string? PrivateReply,
    bool SendPublicReply,
    bool SendPrivateReply,
    bool Enabled,
    int CooldownSeconds,
    int Priority,
    List<MediaForAutomationDto> SelectedMedia,
    int RunsCount,
    int SuccessCount
);

public sealed record MediaForAutomationDto(
    Guid Id,
    string InstagramMediaId,
    string Caption,
    string ThumbnailUrl,
    string MediaUrl,
    DateTime PostedAtUtc
);
