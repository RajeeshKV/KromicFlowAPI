using KromicFlow.Application.Common;
using KromicFlow.Application.DTOs.Automations;
using MediatR;

namespace KromicFlow.Application.Features.Automations.SetAutomationEnabled;

public sealed record SetAutomationEnabledCommand(Guid UserId, Guid Id, bool Enabled) : IRequest<Result<AutomationDto>>;
