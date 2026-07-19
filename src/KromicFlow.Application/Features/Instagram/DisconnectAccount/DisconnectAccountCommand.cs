using KromicFlow.Application.Common;
using KromicFlow.Application.DTOs.Instagram;
using MediatR;

namespace KromicFlow.Application.Features.Instagram.DisconnectAccount;

public sealed record DisconnectAccountCommand(Guid AccountId) : IRequest<Result<InstagramAccountDto>>;
