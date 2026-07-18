using KromicFlow.Application.DTOs.Automations;
using MediatR;

namespace KromicFlow.Application.Features.Automations.ListAutomations;

public sealed record ListAutomationsQuery(Guid UserId) : IRequest<IReadOnlyList<AutomationDto>>;
