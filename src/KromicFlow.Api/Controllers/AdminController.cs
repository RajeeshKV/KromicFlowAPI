using KromicFlow.Api.Contracts.Admin;
using KromicFlow.Application.Features.Admin.Audit;
using KromicFlow.Application.Features.Admin.Notifications;
using KromicFlow.Application.Features.Admin.Plans;
using KromicFlow.Application.Features.Admin.Restrictions;
using KromicFlow.Application.Features.Admin.Settings;
using KromicFlow.Application.Features.Admin.Users;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace KromicFlow.Api.Controllers;

[Authorize(Policy = "AdminOnly")]
[Route("api/v1/admin")]
public sealed class AdminController(IMediator mediator) : ApiControllerBase
{
    [HttpGet("users")]
    public async Task<IActionResult> Users([FromQuery] int page = 1, [FromQuery] int pageSize = 25, CancellationToken cancellationToken = default) =>
        Ok(await mediator.Send(new ListUsersQuery(page, pageSize), cancellationToken));

    [HttpGet("audit")]
    public async Task<IActionResult> Audit([FromQuery] int page = 1, [FromQuery] int pageSize = 25, CancellationToken cancellationToken = default) =>
        Ok(await mediator.Send(new ListAuditLogsQuery(page, pageSize), cancellationToken));

    [HttpGet("restrictions")]
    public async Task<IActionResult> ListRestrictions([FromQuery] int page = 1, [FromQuery] int pageSize = 25, CancellationToken cancellationToken = default) =>
        Ok(await mediator.Send(new ListUserRestrictionsQuery(page, pageSize), cancellationToken));

    [HttpGet("plans")]
    public async Task<IActionResult> ListPlans([FromQuery] int page = 1, [FromQuery] int pageSize = 25, CancellationToken cancellationToken = default) =>
        Ok(await mediator.Send(new ListPlansQuery(page, pageSize), cancellationToken));

    [HttpGet("settings")]
    public async Task<IActionResult> ListSettings([FromQuery] int page = 1, [FromQuery] int pageSize = 50, CancellationToken cancellationToken = default) =>
        Ok(await mediator.Send(new ListRuntimeSettingsQuery(page, pageSize), cancellationToken));

    [HttpGet("notifications")]
    public async Task<IActionResult> ListNotifications([FromQuery] int page = 1, [FromQuery] int pageSize = 25, CancellationToken cancellationToken = default) =>
        Ok(await mediator.Send(new ListNotificationsQuery(page, pageSize), cancellationToken));

    [HttpPost("users/{userId:guid}/restriction")]
    public async Task<IActionResult> RestrictUser(Guid userId, [FromBody] UserRestrictionRequest request, CancellationToken cancellationToken) =>
        FromResult(await mediator.Send(new SetUserRestrictionCommand(User.GetSubjectId(), userId, request.LoginBlocked, request.AutomationBlocked, request.NotificationBlocked, request.Reason), cancellationToken));

    [HttpPost("plans")]
    public async Task<IActionResult> UpsertPlan([FromBody] PlanRequest request, CancellationToken cancellationToken) =>
        FromResult(await mediator.Send(new UpsertPlanCommand(User.GetSubjectId(), request.Code, request.Name, request.IsActive, request.IsDefault, request.MaxInstagramAccounts, request.MaxAutomations, request.MonthlyAutomationRuns, request.MonthlyEmails, request.MonthlyPushNotifications), cancellationToken));

    [HttpPost("settings")]
    public async Task<IActionResult> UpsertSetting([FromBody] RuntimeSettingRequest request, CancellationToken cancellationToken) =>
        FromResult(await mediator.Send(new UpsertRuntimeSettingCommand(User.GetSubjectId(), request.Key, request.Value, request.IsSecret, request.Description), cancellationToken));

    [HttpPost("broadcast")]
    public async Task<IActionResult> Broadcast([FromBody] NotificationRequest request, CancellationToken cancellationToken) =>
        FromResult(await mediator.Send(new SendNotificationCommand(User.GetSubjectId(), null, request.Channel, request.Subject, request.Body), cancellationToken));

    [HttpPost("users/{userId:guid}/notification")]
    public async Task<IActionResult> NotifyUser(Guid userId, [FromBody] NotificationRequest request, CancellationToken cancellationToken) =>
        FromResult(await mediator.Send(new SendNotificationCommand(User.GetSubjectId(), userId, request.Channel, request.Subject, request.Body), cancellationToken));
}

