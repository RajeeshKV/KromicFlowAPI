using KromicFlow.Application.Abstractions;
using KromicFlow.Application.Features.Automations.CreateAutomation;
using KromicFlow.Application.Services;
using KromicFlow.Domain.Entities;
using KromicFlow.Domain.Enums;
using KromicFlow.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace KromicFlow.Tests.Application;

public sealed class CreateAutomationCommandHandlerTests
{
    [Fact]
    public async Task Handle_Fails_WhenPlanAutomationLimitReached()
    {
        var options = new DbContextOptionsBuilder<KromicFlowDbContext>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options;
        await using var db = new KromicFlowDbContext(options);
        var plan = new Plan { Code = "free", Name = "Free", MaxAutomations = 1 };
        var user = new User { Email = "user@example.com", FullName = "User", Plan = plan };
        var account = new InstagramAccount { User = user, InstagramUserId = "ig-1", Username = "user", AccessTokenEncrypted = "token" };
        db.Plans.Add(plan);
        db.Users.Add(user);
        db.InstagramAccounts.Add(account);
        db.Automations.Add(new Automation { InstagramAccount = account, Name = "Existing" });
        await db.SaveChangesAsync();

        var handler = new CreateAutomationCommandHandler(db, new NoopAuditWriter(), new NoopAutomationScopeService());
        var result = await handler.Handle(new CreateAutomationCommand(user.Id, account.Id, "New", AutomationScope.SpecificPosts, "CommentKeyword", ["hi"], "hello", null, 0, 0, []), CancellationToken.None);

        Assert.False(result.Succeeded);
        Assert.Equal("Plan automation limit reached.", result.Error);
    }

    private sealed class NoopAuditWriter : IAuditWriter
    {
        public Task WriteAsync(string action, string entityName, string? entityId, Guid? actorUserId, Guid? actorAdminId, string? detailsJson, CancellationToken cancellationToken) => Task.CompletedTask;
    }

    private sealed class NoopAutomationScopeService : IAutomationScopeService
    {
        public Task<bool> IsAutomationApplicableAsync(Guid automationId, string instagramMediaId, CancellationToken cancellationToken) => Task.FromResult(true);
        public Task<bool> ValidateAutomationScopeAsync(AutomationScope scope, Guid automationId, List<Guid> selectedMediaIds, CancellationToken cancellationToken) => Task.FromResult(true);
    }
}
