using KromicFlow.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace KromicFlow.Api.Configuration;

public static class WebApplicationExtensions
{
    public static async Task ApplyMigrationsAsync(this WebApplication app)
    {
        using var scope = app.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<KromicFlowDbContext>();
        await db.Database.MigrateAsync();
    }

    public static WebApplication UseApiPipeline(this WebApplication app)
    {
        app.UseMiddleware<KromicFlow.Api.Middleware.ExceptionHandlingMiddleware>();
        app.UseSwagger();
        app.UseSwaggerUI();
        app.UseCors("Frontend");
        app.UseRateLimiter();
        app.UseAuthentication();
        app.UseAuthorization();
        app.MapControllers().RequireRateLimiting("api");
        app.MapGet("/api/health", () => Results.Ok(new { status = "healthy", utc = DateTime.UtcNow }));
        app.MapGet("/", () => Results.Redirect("/api/health"));
        return app;
    }
}
