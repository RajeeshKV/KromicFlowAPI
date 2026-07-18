using KromicFlow.Application.Common;
using KromicFlow.Application.DTOs.Admin;
using MediatR;

namespace KromicFlow.Application.Features.Admin.Users;

public sealed record ListUsersQuery(int Page, int PageSize) : IRequest<PagedResult<AdminUserListItemDto>>;
