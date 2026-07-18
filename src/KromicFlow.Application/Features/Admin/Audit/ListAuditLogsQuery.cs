using KromicFlow.Application.Common;
using KromicFlow.Application.DTOs.Admin;
using MediatR;

namespace KromicFlow.Application.Features.Admin.Audit;

public sealed record ListAuditLogsQuery(int Page, int PageSize) : IRequest<PagedResult<AuditLogDto>>;
