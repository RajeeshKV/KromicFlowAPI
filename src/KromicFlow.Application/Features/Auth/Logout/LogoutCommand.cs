using KromicFlow.Application.Common;
using MediatR;

namespace KromicFlow.Application.Features.Auth.Logout;

public sealed record LogoutCommand(Guid SessionGuid) : IRequest<Result>;
