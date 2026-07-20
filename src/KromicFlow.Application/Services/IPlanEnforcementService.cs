namespace KromicFlow.Application.Services;

/// <summary>
/// Returns the effective limits for a user's plan.
/// When enforcement is disabled (enforcement:plans_enabled = false in RuntimeSettings),
/// all limits are returned as int.MaxValue so nothing is ever blocked.
/// Flip the flag to true to enforce real plan limits without any code change.
/// </summary>
public interface IPlanEnforcementService
{
    /// <summary>Whether plan enforcement is currently active.</summary>
    Task<bool> IsEnforcementEnabledAsync(CancellationToken cancellationToken = default);

    Task<int> GetMaxAutomationsAsync(Guid userId, CancellationToken cancellationToken = default);
    Task<int> GetMaxInstagramAccountsAsync(Guid userId, CancellationToken cancellationToken = default);
    Task<int> GetMonthlyAutomationRunsAsync(Guid userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Returns true if the user is within their monthly run quota.
    /// Always returns true when enforcement is disabled.
    /// </summary>
    Task<bool> IsWithinMonthlyRunLimitAsync(Guid userId, int currentMonthRuns, CancellationToken cancellationToken = default);
}
