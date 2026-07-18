using KromicFlow.Application.Abstractions;
using KromicFlow.Application.Common;
using KromicFlow.Application.DTOs.Admin;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace KromicFlow.Application.Features.Admin.Audit;

internal sealed class ListAuditLogsQueryHandler(IKromicFlowDbContext db) : IRequestHandler<ListAuditLogsQuery, PagedResult<AuditLogDto>>
{
    public async Task<PagedResult<AuditLogDto>> Handle(ListAuditLogsQuery request, CancellationToken cancellationToken)
    {
        var page = Math.Max(1, request.Page);
        var pageSize = Math.Clamp(request.PageSize, 1, 100);
        var query = db.AuditLogs.OrderByDescending(x => x.CreatedUtc);
        var total = await query.CountAsync(cancellationToken);
        var items = await query.Skip((page - 1) * pageSize).Take(pageSize).Select(x => new AuditLogDto(x.Id, x.Action, x.EntityName, x.EntityId, x.DetailsJson, x.CreatedUtc)).ToListAsync(cancellationToken);
        return new PagedResult<AuditLogDto>(items, page, pageSize, total);
    }
}
