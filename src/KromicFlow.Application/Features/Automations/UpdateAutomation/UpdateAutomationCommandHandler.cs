using KromicFlow.Application.Abstractions;
using KromicFlow.Application.Common;
using KromicFlow.Application.DTOs.Automations;
using KromicFlow.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace KromicFlow.Application.Features.Automations.UpdateAutomation;

internal sealed class UpdateAutomationCommandHandler(IKromicFlowDbContext db, IAuditWriter auditWriter) : IRequestHandler<UpdateAutomationCommand, Result<AutomationDto>>
{
    public async Task<Result<AutomationDto>> Handle(UpdateAutomationCommand request, CancellationToken cancellationToken)
    {
        var automation = await db.Automations.Include(x => x.InstagramAccount).FirstOrDefaultAsync(x => x.Id == request.Id && x.InstagramAccount.UserId == request.UserId, cancellationToken);
        if (automation is null) return Result<AutomationDto>.Failure("Automation not found.");
        AutomationMapping.Apply(automation, request.Name, request.TriggerType, request.Keywords, request.PublicReply, request.PrivateReply, request.CooldownSeconds, request.Priority);
        automation.UpdatedUtc = DateTime.UtcNow;
        await db.SaveChangesAsync(cancellationToken);
        await auditWriter.WriteAsync("AutomationUpdated", nameof(Automation), automation.Id.ToString(), request.UserId, null, null, cancellationToken);
        return Result<AutomationDto>.Success(AutomationMapping.ToDto(automation));
    }
}
