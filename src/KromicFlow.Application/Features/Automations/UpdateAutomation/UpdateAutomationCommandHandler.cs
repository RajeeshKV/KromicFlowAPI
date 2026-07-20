using KromicFlow.Application.Abstractions;
using KromicFlow.Application.Common;
using KromicFlow.Application.DTOs.Automations;
using KromicFlow.Application.Services;
using KromicFlow.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace KromicFlow.Application.Features.Automations.UpdateAutomation;

internal sealed class UpdateAutomationCommandHandler(IKromicFlowDbContext db, IAuditWriter auditWriter, IAutomationScopeService automationScopeService) : IRequestHandler<UpdateAutomationCommand, Result<AutomationDto>>
{
    public async Task<Result<AutomationDto>> Handle(UpdateAutomationCommand request, CancellationToken cancellationToken)
    {
        var automation = await db.Automations.Include(x => x.InstagramAccount).FirstOrDefaultAsync(x => x.Id == request.Id && x.InstagramAccount.UserId == request.UserId, cancellationToken);
        if (automation is null) return Result<AutomationDto>.Failure("Automation not found.");

        // Validate automation scope
        var scopeValid = await automationScopeService.ValidateAutomationScopeAsync(request.Scope, request.Id, request.SelectedMediaIds, cancellationToken);
        if (!scopeValid) return Result<AutomationDto>.Failure("Invalid automation scope configuration.");

        AutomationMapping.Apply(automation, request.Name, request.Scope, request.TriggerType, request.Keywords, request.PublicReply, request.PrivateReply, request.SendPublicReply, request.SendPrivateReply, request.CooldownSeconds, request.Priority);
        automation.UpdatedUtc = DateTime.UtcNow;

        // Update media mappings for SpecificPosts scope
        var existingMediaMappings = await db.AutomationMedia.Where(x => x.AutomationId == automation.Id).ToListAsync(cancellationToken);
        db.AutomationMedia.RemoveRange(existingMediaMappings);

        if (request.Scope == Domain.Enums.AutomationScope.SpecificPosts && request.SelectedMediaIds.Count > 0)
        {
            foreach (var mediaId in request.SelectedMediaIds)
            {
                db.AutomationMedia.Add(new AutomationMedia
                {
                    AutomationId = automation.Id,
                    InstagramMediaId = mediaId,
                    CreatedAtUtc = DateTime.UtcNow
                });
            }
        }

        await db.SaveChangesAsync(cancellationToken);
        await auditWriter.WriteAsync("AutomationUpdated", nameof(Automation), automation.Id.ToString(), request.UserId, null, null, cancellationToken);
        return Result<AutomationDto>.Success(await AutomationMapping.ToDtoAsync(automation, db, cancellationToken));
    }
}
