using KromicFlow.Application.Abstractions;
using KromicFlow.Application.DTOs.Automations;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace KromicFlow.Application.Features.Automations.ListAutomations;

internal sealed class ListAutomationsQueryHandler(IKromicFlowDbContext db) : IRequestHandler<ListAutomationsQuery, IReadOnlyList<AutomationDto>>
{
    public async Task<IReadOnlyList<AutomationDto>> Handle(ListAutomationsQuery request, CancellationToken cancellationToken) =>
        await db.Automations.Include(x => x.InstagramAccount)
            .Where(x => x.InstagramAccount.UserId == request.UserId)
            .OrderByDescending(x => x.CreatedUtc)
            .Select(x => AutomationMapping.ToDto(x))
            .ToListAsync(cancellationToken);
}
