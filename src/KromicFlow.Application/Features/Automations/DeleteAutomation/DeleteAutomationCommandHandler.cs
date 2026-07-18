using KromicFlow.Application.Abstractions;
using KromicFlow.Application.Common;
using KromicFlow.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace KromicFlow.Application.Features.Automations.DeleteAutomation;

internal sealed class DeleteAutomationCommandHandler(IKromicFlowDbContext db, IAuditWriter auditWriter) : IRequestHandler<DeleteAutomationCommand, Result>
{
    public async Task<Result> Handle(DeleteAutomationCommand request, CancellationToken cancellationToken)
    {
        var automation = await db.Automations.Include(x => x.InstagramAccount).FirstOrDefaultAsync(x => x.Id == request.Id && x.InstagramAccount.UserId == request.UserId, cancellationToken);
        if (automation is null) return Result.Failure("Automation not found.");
        db.Automations.Remove(automation);
        await db.SaveChangesAsync(cancellationToken);
        await auditWriter.WriteAsync("AutomationDeleted", nameof(Automation), automation.Id.ToString(), request.UserId, null, null, cancellationToken);
        return Result.Success();
    }
}
