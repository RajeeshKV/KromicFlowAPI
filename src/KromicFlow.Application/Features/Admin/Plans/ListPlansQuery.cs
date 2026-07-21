using KromicFlow.Application.Common;
using KromicFlow.Application.DTOs.Admin;
using MediatR;

namespace KromicFlow.Application.Features.Admin.Plans;

public sealed record ListPlansQuery(int Page = 1, int PageSize = 25) : IRequest<PagedResult<PlanDto>>;

