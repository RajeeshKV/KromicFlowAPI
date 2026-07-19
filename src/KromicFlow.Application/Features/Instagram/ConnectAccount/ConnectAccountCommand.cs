using KromicFlow.Application.Common;
using KromicFlow.Application.DTOs.Instagram;
using MediatR;

namespace KromicFlow.Application.Features.Instagram.ConnectAccount;

public sealed record ConnectAccountCommand(Guid AccountId) : IRequest<Result<InstagramAccountDto>>;
