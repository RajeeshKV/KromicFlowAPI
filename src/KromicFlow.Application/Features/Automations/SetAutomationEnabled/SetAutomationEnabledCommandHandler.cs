using KromicFlow.Application.Abstractions;
using KromicFlow.Application.Common;
using KromicFlow.Application.DTOs.Automations;
using KromicFlow.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace KromicFlow.Application.Features.Automations.SetAutomationEnabled;

internal sealed class SetAutomationEnabledCommandHandler(IKromicFlowDbContext db, IAuditWriter auditWriter) : IRequestHandler<SetAutomationEnabledCommand, Result<AutomationDto>>
{
    public async Task<Result<AutomationDto>> Handle(SetAutomationEnabledCommand request, CancellationToken cancellationToken)
    {
        var automation = await db.Automations.Include(x => x.InstagramAccount).FirstOrDefaultAsync(x => x.Id == request.Id && x.InstagramAccount.UserId == request.UserId, cancellationToken);
        if (automation is null) return Result<AutomationDto>.Failure("Automation not found.");
        automation.Enabled = request.Enabled;
        automation.UpdatedUtc = DateTime.UtcNow;
        await db.SaveChangesAsync(cancellationToken);
        await auditWriter.WriteAsync(request.Enabled ? "AutomationEnabled" : "AutomationDisabled", nameof(Automation), automation.Id.ToString(), request.UserId, null, null, cancellationToken);
        return Result<AutomationDto>.Success(AutomationMapping.ToDto(automation));
    }
}
