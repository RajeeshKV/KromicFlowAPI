using KromicFlow.Application.Abstractions;
using KromicFlow.Application.Common;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace KromicFlow.Application.Features.Admin.Restrictions;

internal sealed class ListUserRestrictionsQueryHandler(IKromicFlowDbContext db) : IRequestHandler<ListUserRestrictionsQuery, PagedResult<UserRestrictionDto>>
{
    public async Task<PagedResult<UserRestrictionDto>> Handle(ListUserRestrictionsQuery request, CancellationToken cancellationToken)
    {
        var page = Math.Max(1, request.Page);
        var pageSize = Math.Clamp(request.PageSize, 1, 100);
        
        // Only show restrictions where at least one restriction is active
        var query = db.UserRestrictions
            .Where(x => x.LoginBlocked || x.AutomationBlocked || x.NotificationBlocked)
            .OrderByDescending(x => x.UpdatedUtc);
        
        var total = await query.CountAsync(cancellationToken);
        
        var items = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(x => new UserRestrictionDto(x.Id, x.UserId, x.LoginBlocked, x.AutomationBlocked, x.NotificationBlocked, x.Reason, x.SetByAdminId, x.UpdatedUtc ?? DateTime.UtcNow))
            .ToListAsync(cancellationToken);
        
        return new PagedResult<UserRestrictionDto>(items, page, pageSize, total);
    }
}

