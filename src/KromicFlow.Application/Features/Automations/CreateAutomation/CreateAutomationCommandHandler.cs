using KromicFlow.Application.Abstractions;
using KromicFlow.Application.Common;
using KromicFlow.Application.DTOs.Automations;
using KromicFlow.Application.Services;
using KromicFlow.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace KromicFlow.Application.Features.Automations.CreateAutomation;

internal sealed class CreateAutomationCommandHandler(IKromicFlowDbContext db, IAuditWriter auditWriter, IAutomationScopeService automationScopeService) : IRequestHandler<CreateAutomationCommand, Result<AutomationDto>>
{
    public async Task<Result<AutomationDto>> Handle(CreateAutomationCommand request, CancellationToken cancellationToken)
    {
        var account = await db.InstagramAccounts.Include(x => x.User).ThenInclude(x => x.Plan).FirstOrDefaultAsync(x => x.Id == request.InstagramAccountId && x.UserId == request.UserId, cancellationToken);
        if (account is null) return Result<AutomationDto>.Failure("Instagram account not found.");
        if (await db.UserRestrictions.AnyAsync(x => x.UserId == request.UserId && x.AutomationBlocked, cancellationToken)) return Result<AutomationDto>.Failure("Automations are restricted for this user.");
        var count = await db.Automations.CountAsync(x => x.InstagramAccount.UserId == request.UserId, cancellationToken);
        if (count >= account.User.Plan.MaxAutomations) return Result<AutomationDto>.Failure("Plan automation limit reached.");

        // Validate automation scope
        var scopeValid = await automationScopeService.ValidateAutomationScopeAsync(request.Scope, Guid.Empty, request.SelectedMediaIds, cancellationToken);
        if (!scopeValid) return Result<AutomationDto>.Failure("Invalid automation scope configuration.");

        var automation = new Automation { InstagramAccountId = request.InstagramAccountId };
        AutomationMapping.Apply(automation, request.Name, request.Scope, request.TriggerType, request.Keywords, request.PublicReply, request.PrivateReply, request.SendPublicReply, request.SendPrivateReply, request.CooldownSeconds, request.Priority);
        db.Automations.Add(automation);
        await db.SaveChangesAsync(cancellationToken);

        // Add media mappings for SpecificPosts scope
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
            await db.SaveChangesAsync(cancellationToken);
        }

        await auditWriter.WriteAsync("AutomationCreated", nameof(Automation), automation.Id.ToString(), request.UserId, null, null, cancellationToken);
        return Result<AutomationDto>.Success(await AutomationMapping.ToDtoAsync(automation, db, cancellationToken));
    }
}
