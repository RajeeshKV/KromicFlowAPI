using KromicFlow.Application.Abstractions;
using KromicFlow.Application.Options;
using KromicFlow.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace KromicFlow.Infrastructure.Background;

public sealed class TokenRefreshBackgroundService(
    IKromicFlowDbContext db,
    IMetaApiClient metaApiClient,
    IDataProtectionService dataProtectionService,
    ILogger<TokenRefreshBackgroundService> logger) : BackgroundService
{
    private static readonly TimeSpan RefreshInterval = TimeSpan.FromDays(1);
    private static readonly TimeSpan TokenExpiryThreshold = TimeSpan.FromDays(50);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("Token Refresh Background Service started");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await RefreshExpiringTokensAsync(stoppingToken);
                await Task.Delay(RefreshInterval, stoppingToken);
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error in Token Refresh Background Service");
                await Task.Delay(TimeSpan.FromHours(1), stoppingToken);
            }
        }

        logger.LogInformation("Token Refresh Background Service stopped");
    }

    private async Task RefreshExpiringTokensAsync(CancellationToken cancellationToken)
    {
        logger.LogInformation("Starting token refresh check");

        var thresholdDate = DateTime.UtcNow.Add(TokenExpiryThreshold);
        var accountsToRefresh = await db.InstagramAccounts
            .Where(x => x.RefreshRequired || (x.TokenExpiresUtc.HasValue && x.TokenExpiresUtc < thresholdDate))
            .ToListAsync(cancellationToken);

        if (accountsToRefresh.Count == 0)
        {
            logger.LogInformation("No tokens require refresh");
            return;
        }

        logger.LogInformation("Found {Count} tokens to refresh", accountsToRefresh.Count);

        var successCount = 0;
        var failureCount = 0;

        foreach (var account in accountsToRefresh)
        {
            try
            {
                var currentToken = dataProtectionService.Unprotect(account.AccessTokenEncrypted);
                var newToken = await metaApiClient.RefreshLongLivedTokenAsync(currentToken, cancellationToken);
                var encryptedNewToken = dataProtectionService.Protect(newToken);

                account.AccessTokenEncrypted = encryptedNewToken;
                account.RefreshRequired = false;
                account.TokenExpiresUtc = DateTime.UtcNow.AddDays(60);
                account.UpdatedUtc = DateTime.UtcNow;

                successCount++;
                logger.LogInformation("Successfully refreshed token for Instagram account {InstagramUserId}", account.InstagramUserId);
            }
            catch (Exception ex)
            {
                failureCount++;
                account.RefreshRequired = true;
                logger.LogError(ex, "Failed to refresh token for Instagram account {InstagramUserId}", account.InstagramUserId);
            }
        }

        await db.SaveChangesAsync(cancellationToken);
        logger.LogInformation("Token refresh completed: {SuccessCount} succeeded, {FailureCount} failed", successCount, failureCount);
    }
}
