using KromicFlow.Application.Common;

namespace KromicFlow.Application.Services;

public interface IAutomationEligibilityService
{
    Task<Result> ValidateAccountEligibilityAsync(Guid userId, Guid instagramAccountId, CancellationToken cancellationToken = default);
}
