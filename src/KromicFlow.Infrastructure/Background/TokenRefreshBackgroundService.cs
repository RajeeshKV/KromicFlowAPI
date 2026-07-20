using KromicFlow.Application.Abstractions;
using KromicFlow.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace KromicFlow.Infrastructure.Background;

public sealed class TokenRefreshBackgroundService(
    IServiceScopeFactory scopeFactory,
    ILogger<TokenRefreshBackgroundService> logger) : BackgroundService
{
    private static readonly TimeSpan RefreshInterval = TimeSpan.FromHours(12);
    // Refresh tokens that expire within 10 days — Instagram tokens last 60 days
    private static readonly TimeSpan TokenExpiryThreshold = TimeSpan.FromDays(10);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("Token Refresh Background Service started (interval: {Interval}h)", RefreshInterval.TotalHours);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await RefreshExpiringTokensAsync(stoppingToken);
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Unhandled error in Token Refresh Background Service");
            }

            await Task.Delay(RefreshInterval, stoppingToken);
        }

        logger.LogInformation("Token Refresh Background Service stopped");
    }

    private async Task RefreshExpiringTokensAsync(CancellationToken cancellationToken)
    {
        logger.LogInformation("Starting token refresh check");

        using var scope = scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<IKromicFlowDbContext>();
        var metaApiClient = scope.ServiceProvider.GetRequiredService<IMetaApiClient>();
        var dataProtection = scope.ServiceProvider.GetRequiredService<IDataProtectionService>();

        var thresholdDate = DateTime.UtcNow.Add(TokenExpiryThreshold);

        var accountsToRefresh = await db.InstagramAccounts
            .Where(x => x.IsConnected &&
                        x.TokenStatus == "active" &&
                        (x.RefreshRequired ||
                         (x.TokenExpiresUtc.HasValue && x.TokenExpiresUtc.Value < thresholdDate)))
            .ToListAsync(cancellationToken);

        if (accountsToRefresh.Count == 0)
        {
            logger.LogInformation("No tokens require refresh");
            return;
        }

        logger.LogInformation("Found {Count} token(s) to refresh", accountsToRefresh.Count);

        int successCount = 0;
        int failureCount = 0;

        foreach (var account in accountsToRefresh)
        {
            try
            {
                var currentToken = dataProtection.Unprotect(account.AccessTokenEncrypted);
                var newToken = await metaApiClient.RefreshLongLivedTokenAsync(currentToken, cancellationToken);

                account.AccessTokenEncrypted = dataProtection.Protect(newToken);
                account.TokenExpiresUtc = DateTime.UtcNow.AddDays(60);
                account.LastTokenRefreshUtc = DateTime.UtcNow;
                account.TokenStatus = "active";
                account.RefreshRequired = false;
                account.UpdatedUtc = DateTime.UtcNow;

                successCount++;
                logger.LogInformation("Token refreshed for account {InstagramUserId}", account.InstagramUserId);
            }
            catch (System.Security.Cryptography.CryptographicException ex)
            {
                // Token was encrypted with an old key — user must re-authenticate
                failureCount++;
                account.TokenStatus = "invalid";
                account.RefreshRequired = true;
                account.UpdatedUtc = DateTime.UtcNow;
                logger.LogError(ex, "Cannot decrypt token for account {InstagramUserId} — key ring mismatch, re-auth required", account.InstagramUserId);
            }
            catch (Exception ex)
            {
                failureCount++;
                account.RefreshRequired = true;
                account.UpdatedUtc = DateTime.UtcNow;
                logger.LogError(ex, "Failed to refresh token for account {InstagramUserId}", account.InstagramUserId);
            }
        }

        await db.SaveChangesAsync(cancellationToken);
        logger.LogInformation("Token refresh complete — {Success} succeeded, {Failed} failed", successCount, failureCount);
    }
}
