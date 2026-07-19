using KromicFlow.Application.Features.User.DeleteUser;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace KromicFlow.Api.Controllers;

[Authorize]
[Route("api/v1/user")]
public sealed class UserController(IMediator mediator) : ApiControllerBase
{
    [HttpDelete("account")]
    public async Task<IActionResult> DeleteAccount(CancellationToken cancellationToken) =>
        FromResult(await mediator.Send(new DeleteUserCommand(), cancellationToken));
}
