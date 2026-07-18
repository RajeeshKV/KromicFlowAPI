using KromicFlow.Application.Common;
using MediatR;

namespace KromicFlow.Application.Features.Auth.Logout;

public sealed record LogoutAllCommand(Guid ActorId, bool IsAdmin) : IRequest<Result>;
