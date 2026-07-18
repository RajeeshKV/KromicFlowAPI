using KromicFlow.Application.Abstractions;
using KromicFlow.Application.Common;
using KromicFlow.Application.DTOs.Admin;
using KromicFlow.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace KromicFlow.Application.Features.Admin.Plans;

internal sealed class UpsertPlanCommandHandler(IKromicFlowDbContext db, IAuditWriter auditWriter) : IRequestHandler<UpsertPlanCommand, Result<PlanDto>>
{
    public async Task<Result<PlanDto>> Handle(UpsertPlanCommand request, CancellationToken cancellationToken)
    {
        var code = request.Code.Trim().ToLowerInvariant();
        var plan = await db.Plans.FirstOrDefaultAsync(x => x.Code == code, cancellationToken);
        if (plan is null)
        {
            plan = new Plan { Code = code };
            db.Plans.Add(plan);
        }
        if (request.IsDefault)
        {
            var defaultPlans = await db.Plans.Where(x => x.IsDefault).ToListAsync(cancellationToken);
            foreach (var defaultPlan in defaultPlans) defaultPlan.IsDefault = false;
        }
        plan.Name = request.Name;
        plan.IsActive = request.IsActive;
        plan.IsDefault = request.IsDefault;
        plan.MaxInstagramAccounts = request.MaxInstagramAccounts;
        plan.MaxAutomations = request.MaxAutomations;
        plan.MonthlyAutomationRuns = request.MonthlyAutomationRuns;
        plan.MonthlyEmails = request.MonthlyEmails;
        plan.MonthlyPushNotifications = request.MonthlyPushNotifications;
        await db.SaveChangesAsync(cancellationToken);
        await auditWriter.WriteAsync("PlanUpserted", nameof(Plan), plan.Id.ToString(), null, request.AdminId, null, cancellationToken);
        return Result<PlanDto>.Success(new PlanDto(plan.Id, plan.Code, plan.Name, plan.IsActive, plan.IsDefault, plan.MaxInstagramAccounts, plan.MaxAutomations, plan.MonthlyAutomationRuns, plan.MonthlyEmails, plan.MonthlyPushNotifications));
    }
}
