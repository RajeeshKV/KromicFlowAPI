using KromicFlow.Application.Abstractions;
using KromicFlow.Application.Common;
using KromicFlow.Application.DTOs.Admin;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace KromicFlow.Application.Features.Admin.Notifications;

internal sealed class ListNotificationsQueryHandler(IKromicFlowDbContext db) : IRequestHandler<ListNotificationsQuery, PagedResult<NotificationDto>>
{
    public async Task<PagedResult<NotificationDto>> Handle(ListNotificationsQuery request, CancellationToken cancellationToken)
    {
        var page = Math.Max(1, request.Page);
        var pageSize = Math.Clamp(request.PageSize, 1, 100);
        
        var query = db.NotificationMessages.OrderByDescending(x => x.CreatedUtc);
        var total = await query.CountAsync(cancellationToken);
        
        var items = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(x => new NotificationDto(x.Id, x.UserId, x.Audience.ToString(), x.Channel.ToString(), x.Status.ToString(), x.Subject, x.CreatedUtc, x.SentUtc))
            .ToListAsync(cancellationToken);
        
        return new PagedResult<NotificationDto>(items, page, pageSize, total);
    }
}

