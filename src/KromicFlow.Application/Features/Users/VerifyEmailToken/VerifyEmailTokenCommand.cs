using KromicFlow.Application.Common;
using MediatR;

namespace KromicFlow.Application.Features.Users.VerifyEmailToken;

public sealed record VerifyEmailTokenCommand(Guid UserId, string Token) : IRequest<Result>;
