using KromicFlow.Application.Common;
using KromicFlow.Application.DTOs.Admin;
using MediatR;

namespace KromicFlow.Application.Features.Admin.Restrictions;

public sealed record ListUserRestrictionsQuery(int Page = 1, int PageSize = 25) : IRequest<PagedResult<UserRestrictionDto>>;

