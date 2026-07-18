using KromicFlow.Api.Contracts.Auth;
using KromicFlow.Application.Abstractions;
using KromicFlow.Application.Features.Auth.AdminBootstrap;
using KromicFlow.Application.Features.Auth.AdminLogin;
using KromicFlow.Application.Features.Auth.Logout;
using KromicFlow.Application.Features.Auth.MetaCallback;
using KromicFlow.Application.Features.Auth.Refresh;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace KromicFlow.Api.Controllers;

[Route("api/v1/auth")]
public sealed class AuthController(IMediator mediator, IConfiguration configuration, IOAuthStateService oauthStateService) : ApiControllerBase
{
    
    [HttpGet("login")]
    public IActionResult Login()
    {
        var appId = configuration["Meta:AppId"];
        var redirectUri = Uri.EscapeDataString(configuration["Meta:OAuthRedirectUri"] ?? string.Empty);
        var state = oauthStateService.GenerateState();
        
        var url =
        $"https://www.instagram.com/oauth/authorize" +
        $"?enable_fb_login=0" +
        $"&force_authentication=1" +
        $"&client_id={appId}" +
        $"&redirect_uri={redirectUri}" +
        $"&response_type=code" +
        $"&scope=instagram_business_basic,instagram_business_manage_messages,instagram_business_manage_comments" +
        $"&state={state}";

    return Redirect(url);
    }

    [HttpGet("callback")]
    public async Task<IActionResult> Callback([FromQuery] string code, [FromQuery] string state, CancellationToken cancellationToken)
    {
        var redirectUri = Uri.EscapeDataString(configuration["Meta:OAuthRedirectUri"] ?? string.Empty);
        var result = await mediator.Send(new MetaCallbackCommand(code, state, redirectUri, Request.Headers.UserAgent, null, null, HttpContext.Connection.RemoteIpAddress?.ToString()), cancellationToken);
        return FromResult(result);
    }

    [HttpPost("refresh")]
    public async Task<IActionResult> Refresh([FromBody] RefreshRequest request, CancellationToken cancellationToken) =>
        FromResult(await mediator.Send(new RefreshCommand(request.RefreshToken, request.SessionGuid), cancellationToken));

    [Authorize]
    [HttpPost("logout")]
    public async Task<IActionResult> Logout(CancellationToken cancellationToken) =>
        FromResult(await mediator.Send(new LogoutCommand(User.GetSessionGuid()), cancellationToken));

    [Authorize]
    [HttpPost("logout-all")]
    public async Task<IActionResult> LogoutAll(CancellationToken cancellationToken) =>
        FromResult(await mediator.Send(new LogoutAllCommand(User.GetSubjectId(), User.IsAdmin()), cancellationToken));

    [HttpPost("admin/bootstrap")]
    public async Task<IActionResult> BootstrapAdmin([FromBody] AdminBootstrapRequest request, CancellationToken cancellationToken) =>
        FromResult(await mediator.Send(new AdminBootstrapCommand(request.BootstrapKey, request.Username, request.Email, request.Password), cancellationToken));

    [HttpPost("admin/login")]
    public async Task<IActionResult> AdminLogin([FromBody] AdminLoginRequest request, CancellationToken cancellationToken) =>
        FromResult(await mediator.Send(new AdminLoginCommand(request.Username, request.Password, Request.Headers.UserAgent, null, null, HttpContext.Connection.RemoteIpAddress?.ToString()), cancellationToken));
}

