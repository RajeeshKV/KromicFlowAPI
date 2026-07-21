using KromicFlow.Application.Abstractions;
using KromicFlow.Application.Common;
using KromicFlow.Application.DTOs.Admin;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace KromicFlow.Application.Features.Admin.Settings;

internal sealed class ListRuntimeSettingsQueryHandler(IKromicFlowDbContext db) : IRequestHandler<ListRuntimeSettingsQuery, PagedResult<RuntimeSettingDto>>
{
    public async Task<PagedResult<RuntimeSettingDto>> Handle(ListRuntimeSettingsQuery request, CancellationToken cancellationToken)
    {
        var page = Math.Max(1, request.Page);
        var pageSize = Math.Clamp(request.PageSize, 1, 100);
        
        var query = db.RuntimeSettings.OrderBy(x => x.Key);
        var total = await query.CountAsync(cancellationToken);
        
        var items = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(x => new RuntimeSettingDto(x.Key, x.IsSecret ? "***" : x.Value, x.IsSecret, x.Description))
            .ToListAsync(cancellationToken);
        
        return new PagedResult<RuntimeSettingDto>(items, page, pageSize, total);
    }
}

