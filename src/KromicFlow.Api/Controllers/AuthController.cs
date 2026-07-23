using KromicFlow.Api.Contracts.Auth;
using KromicFlow.Application.Abstractions;
using KromicFlow.Application.DTOs.Auth;
using KromicFlow.Application.Features.Auth.AdminBootstrap;
using KromicFlow.Application.Features.Auth.AdminLogin;
using KromicFlow.Application.Features.Auth.Logout;
using KromicFlow.Application.Features.Auth.MetaCallback;
using KromicFlow.Application.Features.Auth.Refresh;
using KromicFlow.Application.Options;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace KromicFlow.Api.Controllers;

[Route("api/v1/auth")]
public sealed class AuthController(
    IMediator mediator,
    IConfiguration configuration,
    IOAuthStateService oauthStateService,
    IOptionsSnapshot<MetaOptions> metaOptions,
    IOptionsSnapshot<PlatformOptions> platformOptions) : ApiControllerBase
{
    /// <summary>
    /// Initiates OAuth login flow for web and mobile clients.
    /// Detects client type and returns appropriate redirect URI.
    /// Query params: ?mobileApp=true (optional) to force mobile redirect
    /// </summary>
    [HttpGet("login")]
    public IActionResult Login([FromQuery] bool mobileApp = false)
    {
        var appId = configuration["Meta:AppId"];
        var state = oauthStateService.GenerateState();
        
        // Determine redirect URI based on client type
        var isMobileClient = mobileApp || DetectMobileClient();
        var redirectUri = isMobileClient 
            ? GetMobileRedirectUri()
            : configuration["Meta:OAuthRedirectUri"] ?? string.Empty;
        
        var url =
            $"https://www.instagram.com/oauth/authorize" +
            $"?enable_fb_login=0" +
            $"&force_authentication=1" +
            $"&client_id={appId}" +
            $"&redirect_uri={Uri.EscapeDataString(redirectUri)}" +
            $"&response_type=code" +
            $"&scope=instagram_business_basic,instagram_business_manage_messages,instagram_business_manage_comments" +
            $"&state={state}";

        return Redirect(url);
    }

    [HttpGet("callback")]
    public async Task<IActionResult> Callback(
        [FromQuery] string code,
        [FromQuery] string state,
        [FromQuery] string? error = null,
        [FromQuery] string? error_description = null,
        CancellationToken cancellationToken = default)
    {
        // Handle OAuth error responses
        if (!string.IsNullOrEmpty(error))
        {
            var errorMsg = error_description ?? error;
            return HandleOAuthError(errorMsg);
        }

        // Determine which redirect URI was used (detect from request context)
        var isMobileClient = DetectMobileClient();
        var redirectUri = isMobileClient
            ? GetMobileRedirectUri()
            : configuration["Meta:OAuthRedirectUri"] ?? string.Empty;

        var result = await mediator.Send(
            new MetaCallbackCommand(
                code,
                state,
                redirectUri,
                Request.Headers.UserAgent,
                null,
                null,
                HttpContext.Connection.RemoteIpAddress?.ToString()),
            cancellationToken);
        
        if (!result.Succeeded)
        {
            return HandleOAuthError(result.Error ?? "Authentication failed");
        }
        
        var tokens = result.Value!.Tokens;
        var profile = result.Value.Profile;

        // Format callback URL based on client type
        if (isMobileClient)
        {
            return HandleMobileCallback(tokens, profile);
        }
        else
        {
            return HandleWebCallback(tokens, profile);
        }
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

    /// <summary>
    /// Gets the appropriate mobile redirect URI.
    /// Returns the registered mobile scheme: kromicflow://auth/callback
    /// </summary>
    private string GetMobileRedirectUri()
    {
        return metaOptions.Value.MobileRedirectUri;
    }

    /// <summary>
    /// Detects if the current request is from a mobile client.
    /// Checks: User-Agent header and HttpContext items set by MobileClientMiddleware
    /// </summary>
    private bool DetectMobileClient()
    {
        // Check if middleware already detected mobile client
        if (HttpContext.Items.TryGetValue("IsMobileClient", out var isMobile) && isMobile is bool && (bool)isMobile)
            return true;

        // Fallback: Check User-Agent
        var userAgent = Request.Headers.UserAgent.ToString();
        return !string.IsNullOrEmpty(userAgent) && (
            userAgent.Contains("ReactNative", StringComparison.OrdinalIgnoreCase) ||
            userAgent.Contains("Expo", StringComparison.OrdinalIgnoreCase) ||
            userAgent.Contains("ExpoGo", StringComparison.OrdinalIgnoreCase));
    }

    private bool IsLocalEnvironment()
    {
        var env = HttpContext.RequestServices.GetRequiredService<IWebHostEnvironment>();
        return env.IsDevelopment();
    }

    /// <summary>
    /// Handles successful OAuth callback for web clients.
    /// Redirects to FrontendRedirectUri with tokens in URL fragment.
    /// </summary>
    private IActionResult HandleWebCallback(AuthTokenDto tokens, UserProfileDto profile)
    {
        var frontendRedirectUri = configuration["Meta:FrontendRedirectUri"] ?? string.Empty;
        var callbackUrl = $"{frontendRedirectUri}#" +
            $"accessToken={Uri.EscapeDataString(tokens.AccessToken)}" +
            $"&refreshToken={Uri.EscapeDataString(tokens.RefreshToken)}" +
            $"&expiresUtc={Uri.EscapeDataString(tokens.ExpiresUtc.ToString("o"))}" +
            $"&sessionGuid={Uri.EscapeDataString(tokens.SessionGuid.ToString())}" +
            $"&userId={Uri.EscapeDataString(profile.Id.ToString())}" +
            $"&email={Uri.EscapeDataString(profile.Email ?? "")}" +
            $"&fullName={Uri.EscapeDataString(profile.FullName)}" +
            $"&role={Uri.EscapeDataString(profile.Role)}";
            
        return Redirect(callbackUrl);
    }

    /// <summary>
    /// Handles successful OAuth callback for mobile clients.
    /// Redirects to mobile deep link scheme with tokens as query parameters.
    /// </summary>
    private IActionResult HandleMobileCallback(AuthTokenDto tokens, UserProfileDto profile)
    {
        var mobileScheme = platformOptions.Value.MobileDeepLinkScheme;
        var callbackPath = platformOptions.Value.MobileOAuthCallbackPath;

        var callbackUrl = $"{mobileScheme}://{callbackPath}?" +
            $"accessToken={Uri.EscapeDataString(tokens.AccessToken)}" +
            $"&refreshToken={Uri.EscapeDataString(tokens.RefreshToken)}" +
            $"&expiresUtc={Uri.EscapeDataString(tokens.ExpiresUtc.ToString("o"))}" +
            $"&sessionGuid={Uri.EscapeDataString(tokens.SessionGuid.ToString())}" +
            $"&userId={Uri.EscapeDataString(profile.Id.ToString())}" +
            $"&email={Uri.EscapeDataString(profile.Email ?? "")}" +
            $"&fullName={Uri.EscapeDataString(profile.FullName)}" +
            $"&role={Uri.EscapeDataString(profile.Role)}";
            
        return Redirect(callbackUrl);
    }

    /// <summary>
    /// Handles OAuth error responses by redirecting to appropriate error page.
    /// For web: Uses FrontendRedirectUri with error in fragment
    /// For mobile: Uses deep link with error in query string
    /// </summary>
    private IActionResult HandleOAuthError(string errorMsg)
    {
        var isMobileClient = DetectMobileClient();
        
        if (isMobileClient)
        {
            var mobileScheme = platformOptions.Value.MobileDeepLinkScheme;
            var errorUrl = $"{mobileScheme}://auth/callback?error={Uri.EscapeDataString(errorMsg)}";
            return Redirect(errorUrl);
        }
        else
        {
            var frontendRedirectUri = configuration["Meta:FrontendRedirectUri"] ?? string.Empty;
            var errorUrl = $"{frontendRedirectUri}?error={Uri.EscapeDataString(errorMsg)}";
            return Redirect(errorUrl);
        }
    }
}


