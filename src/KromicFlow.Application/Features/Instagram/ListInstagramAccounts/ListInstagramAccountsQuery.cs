using KromicFlow.Application.DTOs.Instagram;
using MediatR;

namespace KromicFlow.Application.Features.Instagram.ListInstagramAccounts;

public sealed record ListInstagramAccountsQuery(Guid UserId) : IRequest<IReadOnlyList<InstagramAccountDto>>;
