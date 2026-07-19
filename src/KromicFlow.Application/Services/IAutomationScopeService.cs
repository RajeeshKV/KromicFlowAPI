using KromicFlow.Domain.Enums;

namespace KromicFlow.Application.Services;

public interface IAutomationScopeService
{
    Task<bool> IsAutomationApplicableAsync(
        Guid automationId,
        string instagramMediaId,
        CancellationToken cancellationToken);

    Task<bool> ValidateAutomationScopeAsync(
        AutomationScope scope,
        Guid automationId,
        List<Guid> selectedMediaIds,
        CancellationToken cancellationToken);
}
