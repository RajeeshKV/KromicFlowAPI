using KromicFlow.Api.Contracts.Automations;
using KromicFlow.Application.Features.Automations.CreateAutomation;
using KromicFlow.Application.Features.Automations.DeleteAutomation;
using KromicFlow.Application.Features.Automations.ListAutomations;
using KromicFlow.Application.Features.Automations.SetAutomationEnabled;
using KromicFlow.Application.Features.Automations.UpdateAutomation;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace KromicFlow.Api.Controllers;

[Authorize]
[Route("api/v1/automations")]
public sealed class AutomationsController(IMediator mediator) : ApiControllerBase
{
    [HttpGet]
    public async Task<IActionResult> List(CancellationToken cancellationToken) => Ok(await mediator.Send(new ListAutomationsQuery(User.GetSubjectId()), cancellationToken));

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] AutomationRequest request, CancellationToken cancellationToken) =>
        FromResult(await mediator.Send(new CreateAutomationCommand(User.GetSubjectId(), request.InstagramAccountId, request.Name, request.Scope, request.TriggerType, request.Keywords, request.PublicReply, request.PrivateReply, request.SendPublicReply, request.SendPrivateReply, request.CooldownSeconds, request.Priority, request.SelectedMediaIds), cancellationToken));

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] AutomationRequest request, CancellationToken cancellationToken) =>
        FromResult(await mediator.Send(new UpdateAutomationCommand(User.GetSubjectId(), id, request.Name, request.Scope, request.TriggerType, request.Keywords, request.PublicReply, request.PrivateReply, request.SendPublicReply, request.SendPrivateReply, request.CooldownSeconds, request.Priority, request.SelectedMediaIds), cancellationToken));

    [HttpPatch("{id:guid}/enable")]
    public async Task<IActionResult> Enable(Guid id, [FromBody] EnableAutomationRequest request, CancellationToken cancellationToken) =>
        FromResult(await mediator.Send(new SetAutomationEnabledCommand(User.GetSubjectId(), id, request.Enabled), cancellationToken));

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken) =>
        FromResult(await mediator.Send(new DeleteAutomationCommand(User.GetSubjectId(), id), cancellationToken));
}

