using KromicFlow.Domain.Enums;

namespace KromicFlow.Api.Contracts.Automations;

public sealed record AutomationRequest(
    Guid InstagramAccountId,
    string Name,
    AutomationScope Scope,
    AutomationTriggerType TriggerType,
    string[] Keywords,
    string? PublicReply,
    string? PrivateReply,
    bool SendPublicReply,
    bool SendPrivateReply,
    int CooldownSeconds,
    int Priority,
    List<Guid> SelectedMediaIds
);
