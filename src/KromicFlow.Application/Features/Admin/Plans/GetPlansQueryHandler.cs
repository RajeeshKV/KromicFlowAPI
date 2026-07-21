using KromicFlow.Application.Abstractions;
using KromicFlow.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace KromicFlow.Application.Features.Admin.Plans;

internal sealed class GetPlansQueryHandler(IKromicFlowDbContext db) : IRequestHandler<GetPlansQuery, List<Plan>>
{
    public async Task<List<Plan>> Handle(GetPlansQuery request, CancellationToken cancellationToken)
    {
        return await db.Plans
            .Where(x => x.IsActive)
            .OrderBy(x => x.IsDefault ? 0 : 1)
            .ThenBy(x => x.Code)
            .ToListAsync(cancellationToken);
    }
}

