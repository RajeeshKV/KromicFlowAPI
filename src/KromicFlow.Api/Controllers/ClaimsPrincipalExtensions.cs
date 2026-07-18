using System.Security.Claims;

namespace KromicFlow.Api.Controllers;

internal static class ClaimsPrincipalExtensions
{
    public static Guid GetSubjectId(this ClaimsPrincipal principal) => Guid.Parse(principal.FindFirstValue(ClaimTypes.NameIdentifier) ?? principal.FindFirstValue("sub")!);
    public static Guid GetSessionGuid(this ClaimsPrincipal principal) => Guid.Parse(principal.FindFirstValue("sid")!);
    public static bool IsAdmin(this ClaimsPrincipal principal) => principal.IsInRole("Admin");
}
