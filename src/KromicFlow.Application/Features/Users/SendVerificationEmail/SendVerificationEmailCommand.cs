using KromicFlow.Application.Common;
using MediatR;

namespace KromicFlow.Application.Features.Users.SendVerificationEmail;

public sealed record SendVerificationEmailCommand(Guid UserId, string Email) : IRequest<Result>;
