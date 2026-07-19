using KromicFlow.Application.Features.Instagram.ConnectAccount;
using KromicFlow.Application.Features.Instagram.DisconnectAccount;
using KromicFlow.Application.Features.Instagram.GetInstagramMedia;
using KromicFlow.Application.Features.Instagram.ListInstagramAccounts;
using KromicFlow.Application.Features.Instagram.SyncInstagramAccount;
using KromicFlow.Domain.Enums;
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

    [HttpPost("{id:guid}/connect")]
    public async Task<IActionResult> Connect(Guid id, CancellationToken cancellationToken) =>
        FromResult(await mediator.Send(new ConnectAccountCommand(id), cancellationToken));

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Disconnect(Guid id, CancellationToken cancellationToken) =>
        FromResult(await mediator.Send(new DisconnectAccountCommand(id), cancellationToken));

    [HttpGet("{id:guid}/media")]
    public async Task<IActionResult> Media(
        Guid id,
        CancellationToken cancellationToken,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] string? mediaType = null,
        [FromQuery] string? search = null)
    {
        MediaType? parsedMediaType = null;
        if (!string.IsNullOrWhiteSpace(mediaType) && Enum.TryParse<MediaType>(mediaType, true, out var parsed))
        {
            parsedMediaType = parsed;
        }
        
        return Ok(await mediator.Send(new GetInstagramMediaQuery(id, page, pageSize, parsedMediaType, search), cancellationToken));
    }

    [HttpPost("{id:guid}/sync")]
    public async Task<IActionResult> Sync(Guid id, CancellationToken cancellationToken) =>
        FromResult(await mediator.Send(new SyncInstagramAccountCommand(User.GetSubjectId(), id), cancellationToken));
}
