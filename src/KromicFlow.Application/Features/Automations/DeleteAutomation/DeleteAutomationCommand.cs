using KromicFlow.Application.Common;
using MediatR;

namespace KromicFlow.Application.Features.Automations.DeleteAutomation;

public sealed record DeleteAutomationCommand(Guid UserId, Guid Id) : IRequest<Result>;
