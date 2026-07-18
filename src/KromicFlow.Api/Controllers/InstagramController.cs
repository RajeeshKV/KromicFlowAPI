using KromicFlow.Application.Features.Instagram.ListInstagramAccounts;
using KromicFlow.Application.Features.Instagram.SyncInstagramAccount;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace KromicFlow.Api.Controllers;

[Authorize]
[Route("api/v1/instagram")]
public sealed class InstagramController(IMediator mediator) : ApiControllerBase
{
    [HttpGet("accounts")]
    public async Task<IActionResult> Accounts(CancellationToken cancellationToken) =>
        Ok(await mediator.Send(new ListInstagramAccountsQuery(User.GetSubjectId()), cancellationToken));

    [HttpPost("{id:guid}/sync")]
    public async Task<IActionResult> Sync(Guid id, CancellationToken cancellationToken) =>
        FromResult(await mediator.Send(new SyncInstagramAccountCommand(User.GetSubjectId(), id), cancellationToken));
}
