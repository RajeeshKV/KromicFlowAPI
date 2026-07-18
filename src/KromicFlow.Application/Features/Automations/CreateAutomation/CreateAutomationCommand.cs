using KromicFlow.Application.Common;
using KromicFlow.Application.DTOs.Automations;
using MediatR;

namespace KromicFlow.Application.Features.Automations.CreateAutomation;

public sealed record CreateAutomationCommand(Guid UserId, Guid InstagramAccountId, string Name, string TriggerType, string[] Keywords, string? PublicReply, string? PrivateReply, int CooldownSeconds, int Priority) : IRequest<Result<AutomationDto>>;
