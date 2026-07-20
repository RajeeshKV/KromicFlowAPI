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
    bool Enabled,
    int CooldownSeconds,
    int Priority,
    List<MediaForAutomationDto> SelectedMedia
);

public sealed record MediaForAutomationDto(
    Guid Id,
    string InstagramMediaId,
    string Caption,
    string ThumbnailUrl,
    DateTime PostedAtUtc
);
