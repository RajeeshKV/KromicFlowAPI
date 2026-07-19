using KromicFlow.Application.Abstractions;
using KromicFlow.Application.DTOs.Automations;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace KromicFlow.Application.Features.Automations.ListAutomations;

internal sealed class ListAutomationsQueryHandler(IKromicFlowDbContext db) : IRequestHandler<ListAutomationsQuery, IReadOnlyList<AutomationDto>>
{
    public async Task<IReadOnlyList<AutomationDto>> Handle(ListAutomationsQuery request, CancellationToken cancellationToken)
    {
        var automations = await db.Automations
            .Include(x => x.InstagramAccount)
            .Where(x => x.InstagramAccount.UserId == request.UserId)
            .OrderByDescending(x => x.CreatedUtc)
            .ToListAsync(cancellationToken);

        var dtos = new List<AutomationDto>();
        foreach (var automation in automations)
        {
            dtos.Add(await AutomationMapping.ToDtoAsync(automation, db, cancellationToken));
        }

        return dtos;
    }
}
