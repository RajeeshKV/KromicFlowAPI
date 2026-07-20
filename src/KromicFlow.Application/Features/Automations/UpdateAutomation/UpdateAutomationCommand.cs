using KromicFlow.Application.Common;
using KromicFlow.Application.DTOs.Automations;
using KromicFlow.Domain.Enums;
using MediatR;

namespace KromicFlow.Application.Features.Automations.UpdateAutomation;

public sealed record UpdateAutomationCommand(
    Guid UserId,
    Guid Id,
    string Name,
    AutomationScope Scope,
    AutomationTriggerType TriggerType,
    string[] Keywords,
    string? PublicReply,
    string? PrivateReply,
    int CooldownSeconds,
    int Priority,
    List<Guid> SelectedMediaIds
) : IRequest<Result<AutomationDto>>;
