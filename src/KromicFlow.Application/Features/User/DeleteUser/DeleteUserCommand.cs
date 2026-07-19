using KromicFlow.Application.Common;
using MediatR;

namespace KromicFlow.Application.Features.User.DeleteUser;

public sealed record DeleteUserCommand : IRequest<Result>
{
}
