using KromicFlow.Application.Common;
using KromicFlow.Application.DTOs.Automations;
using MediatR;

namespace KromicFlow.Application.Features.Automations.UpdateAutomation;

public sealed record UpdateAutomationCommand(Guid UserId, Guid Id, string Name, string TriggerType, string[] Keywords, string? PublicReply, string? PrivateReply, int CooldownSeconds, int Priority) : IRequest<Result<AutomationDto>>;
