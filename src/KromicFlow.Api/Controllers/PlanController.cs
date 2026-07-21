using KromicFlow.Application.Features.Admin.Plans;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace KromicFlow.Api.Controllers;

[Route("api/v1/plans")]
public sealed class PlanController(IMediator mediator) : ApiControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetPlans(CancellationToken cancellationToken = default)
    {
        var plans = await mediator.Send(new GetPlansQuery(), cancellationToken);
        return Ok(plans);
    }
}

