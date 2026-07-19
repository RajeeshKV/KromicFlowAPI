using KromicFlow.Application.Abstractions;
using KromicFlow.Domain.Entities;
using KromicFlow.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace KromicFlow.Application.Services;

public sealed class AutomationScopeService(IKromicFlowDbContext db) : IAutomationScopeService
{
    public async Task<bool> IsAutomationApplicableAsync(
        Guid automationId,
        string instagramMediaId,
        CancellationToken cancellationToken)
    {
        var automation = await db.Automations
            .Include(x => x.AutomationMedia)
            .FirstOrDefaultAsync(x => x.Id == automationId, cancellationToken);

        if (automation == null || !automation.Enabled)
        {
            return false;
        }

        var media = await db.InstagramMedia
            .FirstOrDefaultAsync(x => x.InstagramMediaId == instagramMediaId && !x.IsDeleted, cancellationToken);

        if (media == null)
        {
            return false;
        }

        return automation.Scope switch
        {
            AutomationScope.SpecificPosts => await CheckSpecificPostsAsync(automation, media.Id, cancellationToken),
            AutomationScope.ExistingPosts => await CheckExistingPostsAsync(automation, media, cancellationToken),
            AutomationScope.FuturePosts => await CheckFuturePostsAsync(automation, media, cancellationToken),
            AutomationScope.AllPosts => true,
            _ => false
        };
    }

    public async Task<bool> ValidateAutomationScopeAsync(
        AutomationScope scope,
        Guid automationId,
        List<Guid> selectedMediaIds,
        CancellationToken cancellationToken)
    {
        return scope switch
        {
            AutomationScope.SpecificPosts => await ValidateSpecificPostsAsync(selectedMediaIds, cancellationToken),
            AutomationScope.ExistingPosts => await ValidateExistingPostsAsync(automationId, selectedMediaIds, cancellationToken),
            AutomationScope.FuturePosts => await ValidateFuturePostsAsync(selectedMediaIds, cancellationToken),
            AutomationScope.AllPosts => await ValidateAllPostsAsync(selectedMediaIds, cancellationToken),
            _ => false
        };
    }

    private async Task<bool> CheckSpecificPostsAsync(Automation automation, Guid mediaId, CancellationToken cancellationToken)
    {
        return await db.AutomationMedia
            .AnyAsync(x => x.AutomationId == automation.Id && x.InstagramMediaId == mediaId, cancellationToken);
    }

    private async Task<bool> CheckExistingPostsAsync(Automation automation, InstagramMedia media, CancellationToken cancellationToken)
    {
        return media.PostedAtUtc < automation.CreatedUtc;
    }

    private async Task<bool> CheckFuturePostsAsync(Automation automation, InstagramMedia media, CancellationToken cancellationToken)
    {
        return media.PostedAtUtc > automation.CreatedUtc;
    }

    private async Task<bool> ValidateSpecificPostsAsync(List<Guid> selectedMediaIds, CancellationToken cancellationToken)
    {
        // SpecificPosts must have at least one media selected
        if (selectedMediaIds.Count == 0)
        {
            return false;
        }

        // Verify all media exist
        var existingMediaCount = await db.InstagramMedia
            .Where(x => selectedMediaIds.Contains(x.Id) && !x.IsDeleted)
            .CountAsync(cancellationToken);

        return existingMediaCount == selectedMediaIds.Count;
    }

    private async Task<bool> ValidateExistingPostsAsync(Guid automationId, List<Guid> selectedMediaIds, CancellationToken cancellationToken)
    {
        // ExistingPosts should not have media selected
        if (selectedMediaIds.Count > 0)
        {
            return false;
        }

        return true;
    }

    private async Task<bool> ValidateFuturePostsAsync(List<Guid> selectedMediaIds, CancellationToken cancellationToken)
    {
        // FuturePosts should not have media selected
        if (selectedMediaIds.Count > 0)
        {
            return false;
        }

        return true;
    }

    private async Task<bool> ValidateAllPostsAsync(List<Guid> selectedMediaIds, CancellationToken cancellationToken)
    {
        // AllPosts ignores media selection, but we should not have media selected
        if (selectedMediaIds.Count > 0)
        {
            return false;
        }

        return true;
    }
}
