using KromicFlow.Application.Common;
using MediatR;

namespace KromicFlow.Application.Features.Instagram.SyncInstagramAccount;

public sealed record SyncInstagramAccountCommand(Guid UserId, Guid InstagramAccountId) : IRequest<Result>;
