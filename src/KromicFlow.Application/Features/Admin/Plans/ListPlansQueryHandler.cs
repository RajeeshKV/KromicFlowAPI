using KromicFlow.Application.Abstractions;
using KromicFlow.Application.Common;
using KromicFlow.Application.DTOs.Admin;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace KromicFlow.Application.Features.Admin.Plans;

internal sealed class ListPlansQueryHandler(IKromicFlowDbContext db) : IRequestHandler<ListPlansQuery, PagedResult<PlanDto>>
{
    public async Task<PagedResult<PlanDto>> Handle(ListPlansQuery request, CancellationToken cancellationToken)
    {
        var page = Math.Max(1, request.Page);
        var pageSize = Math.Clamp(request.PageSize, 1, 100);
        
        var query = db.Plans.OrderBy(x => x.IsDefault ? 0 : 1).ThenBy(x => x.Code);
        var total = await query.CountAsync(cancellationToken);
        
        var items = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(x => new PlanDto(x.Id, x.Code, x.Name, x.IsActive, x.IsDefault, x.MaxInstagramAccounts, x.MaxAutomations, x.MonthlyAutomationRuns, x.MonthlyEmails, x.MonthlyPushNotifications, x.PriceInrPaise, x.BillingPeriod, x.RazorpayPlanId))
            .ToListAsync(cancellationToken);
        
        return new PagedResult<PlanDto>(items, page, pageSize, total);
    }
}

