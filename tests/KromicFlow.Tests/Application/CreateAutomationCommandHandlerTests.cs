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

        var handler = new CreateAutomationCommandHandler(db, new NoopAuditWriter(), new NoopAutomationScopeService(), new EnforcingPlanEnforcementService(1));
        var result = await handler.Handle(new CreateAutomationCommand(user.Id, account.Id, "New", AutomationScope.SpecificPosts, AutomationTriggerType.CommentKeyword, ["hi"], "hello", null, false, false, 0, 0, []), CancellationToken.None);

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

    // Enforcement stub that always enforces a fixed max limit
    private sealed class EnforcingPlanEnforcementService(int maxAutomations) : IPlanEnforcementService
    {
        public Task<bool> IsEnforcementEnabledAsync(CancellationToken cancellationToken = default) => Task.FromResult(true);
        public Task<int> GetMaxAutomationsAsync(Guid userId, CancellationToken cancellationToken = default) => Task.FromResult(maxAutomations);
        public Task<int> GetMaxInstagramAccountsAsync(Guid userId, CancellationToken cancellationToken = default) => Task.FromResult(int.MaxValue);
        public Task<int> GetMonthlyAutomationRunsAsync(Guid userId, CancellationToken cancellationToken = default) => Task.FromResult(int.MaxValue);
        public Task<bool> IsWithinMonthlyRunLimitAsync(Guid userId, int currentMonthRuns, CancellationToken cancellationToken = default) => Task.FromResult(true);
    }
}
