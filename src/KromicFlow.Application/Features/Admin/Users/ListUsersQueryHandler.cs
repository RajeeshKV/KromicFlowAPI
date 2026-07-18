using KromicFlow.Application.Abstractions;
using KromicFlow.Application.Common;
using KromicFlow.Application.DTOs.Admin;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace KromicFlow.Application.Features.Admin.Users;

internal sealed class ListUsersQueryHandler(IKromicFlowDbContext db) : IRequestHandler<ListUsersQuery, PagedResult<AdminUserListItemDto>>
{
    public async Task<PagedResult<AdminUserListItemDto>> Handle(ListUsersQuery request, CancellationToken cancellationToken)
    {
        var page = Math.Max(1, request.Page);
        var pageSize = Math.Clamp(request.PageSize, 1, 100);
        var query = db.Users.Include(x => x.Plan).Include(x => x.Restriction).OrderByDescending(x => x.CreatedUtc);
        var total = await query.CountAsync(cancellationToken);
        var items = await query.Skip((page - 1) * pageSize).Take(pageSize)
            .Select(x => new AdminUserListItemDto(x.Id, x.Email, x.FullName, x.IsActive, x.Plan.Code, x.CreatedUtc, x.Restriction != null && x.Restriction.LoginBlocked, x.Restriction != null && x.Restriction.AutomationBlocked, x.Restriction != null && x.Restriction.NotificationBlocked))
            .ToListAsync(cancellationToken);
        return new PagedResult<AdminUserListItemDto>(items, page, pageSize, total);
    }
}
