using KromicFlow.Application.Abstractions;
using KromicFlow.Application.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace KromicFlow.Infrastructure.Services;

public sealed class PlanEnforcementService(
    IKromicFlowDbContext db,
    ILogger<PlanEnforcementService> logger) : IPlanEnforcementService
{
    // RuntimeSetting key that controls whether limits are enforced.
    // Value "true" (case-insensitive) = enforce. Anything else = bypass.
    private const string EnforcementKey = "enforcement:plans_enabled";

    public async Task<bool> IsEnforcementEnabledAsync(CancellationToken cancellationToken = default)
    {
        var setting = await db.RuntimeSettings
            .FirstOrDefaultAsync(x => x.Key == EnforcementKey, cancellationToken);

        if (setting is null) return false; // default: enforcement OFF
        return string.Equals(setting.Value, "true", StringComparison.OrdinalIgnoreCase);
    }

    public async Task<int> GetMaxAutomationsAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        if (!await IsEnforcementEnabledAsync(cancellationToken))
            return int.MaxValue;

        return await GetPlanLimitAsync(userId, p => p.MaxAutomations, cancellationToken);
    }

    public async Task<int> GetMaxInstagramAccountsAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        if (!await IsEnforcementEnabledAsync(cancellationToken))
            return int.MaxValue;

        return await GetPlanLimitAsync(userId, p => p.MaxInstagramAccounts, cancellationToken);
    }

    public async Task<int> GetMonthlyAutomationRunsAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        if (!await IsEnforcementEnabledAsync(cancellationToken))
            return int.MaxValue;

        return await GetPlanLimitAsync(userId, p => p.MonthlyAutomationRuns, cancellationToken);
    }

    public async Task<bool> IsWithinMonthlyRunLimitAsync(Guid userId, int currentMonthRuns, CancellationToken cancellationToken = default)
    {
        if (!await IsEnforcementEnabledAsync(cancellationToken))
            return true;

        var limit = await GetPlanLimitAsync(userId, p => p.MonthlyAutomationRuns, cancellationToken);
        var within = currentMonthRuns < limit;

        if (!within)
            logger.LogWarning("User {UserId} has reached monthly automation run limit ({Runs}/{Limit})", userId, currentMonthRuns, limit);

        return within;
    }

    private async Task<int> GetPlanLimitAsync(Guid userId, Func<Domain.Entities.Plan, int> selector, CancellationToken cancellationToken)
    {
        var plan = await db.Users
            .Where(x => x.Id == userId)
            .Select(x => x.Plan)
            .FirstOrDefaultAsync(cancellationToken);

        return plan is null ? int.MaxValue : selector(plan);
    }
}
