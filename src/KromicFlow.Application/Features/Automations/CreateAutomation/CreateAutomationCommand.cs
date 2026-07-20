using KromicFlow.Application.Common;
using KromicFlow.Application.DTOs.Automations;
using KromicFlow.Domain.Enums;
using MediatR;

namespace KromicFlow.Application.Features.Automations.CreateAutomation;

public sealed record CreateAutomationCommand(
    Guid UserId,
    Guid InstagramAccountId,
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
