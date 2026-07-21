using KromicFlow.Application.Features.User.DeleteUser;
using KromicFlow.Application.Features.User.GetUserProfile;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace KromicFlow.Api.Controllers;

[Authorize]
[Route("api/v1/user")]
public sealed class UserController(IMediator mediator) : ApiControllerBase
{
    [HttpGet("profile")]
    public async Task<IActionResult> GetProfile(CancellationToken cancellationToken)
    {
        var profile = await mediator.Send(new GetUserProfileQuery(User.GetSubjectId()), cancellationToken);
        return Ok(profile);
    }

    [HttpDelete("account")]
    public async Task<IActionResult> DeleteAccount(CancellationToken cancellationToken) =>
        FromResult(await mediator.Send(new DeleteUserCommand(), cancellationToken));
}
