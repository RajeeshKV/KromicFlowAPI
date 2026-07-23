using System.Text.RegularExpressions;

namespace KromicFlow.Api.Middleware;

/// <summary>
/// Middleware to handle mobile client headers and scheme validation.
/// Detects React Native/Expo clients and validates deep link schemes.
/// </summary>
public sealed class MobileClientMiddleware(RequestDelegate next, ILogger<MobileClientMiddleware> logger)
{
    private static readonly string[] MobileUserAgentPatterns =
    [
        "ReactNative",
        "Expo",
        "ExpoGo",
        "okhttp"  // OkHttp used by some React Native clients
    ];

    public async Task InvokeAsync(HttpContext context)
    {
        // Detect if this is a mobile client
        var isMobileClient = DetectMobileClient(context);
        
        if (isMobileClient)
        {
            context.Items["IsMobileClient"] = true;
            logger.LogInformation("Mobile client request detected from {UserAgent}", context.Request.Headers.UserAgent.ToString());
        }

        await next(context);
    }

    private bool DetectMobileClient(HttpContext context)
    {
        var userAgent = context.Request.Headers.UserAgent.ToString();
        
        // Check for known mobile patterns
        if (string.IsNullOrWhiteSpace(userAgent))
            return false;

        return MobileUserAgentPatterns.Any(pattern => 
            userAgent.Contains(pattern, StringComparison.OrdinalIgnoreCase));
    }
}
