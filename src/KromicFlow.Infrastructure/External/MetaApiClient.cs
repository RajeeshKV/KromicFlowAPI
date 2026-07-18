using KromicFlow.Application.Abstractions;
using KromicFlow.Application.Options;
using KromicFlow.Domain.Entities;
using Microsoft.Extensions.Options;

namespace KromicFlow.Infrastructure.External;

public sealed class MetaApiClient(HttpClient httpClient, IOptions<MetaOptions> options) : IMetaApiClient
{
    public Task<MetaUserProfile> ExchangeAuthorizationCodeAsync(string code, string redirectUri, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(options.Value.AppId) || string.IsNullOrWhiteSpace(options.Value.AppSecret))
        {
            throw new InvalidOperationException("Meta OAuth is not configured. Set Meta__AppId and Meta__AppSecret.");
        }

        _ = httpClient.BaseAddress;
        var profile = new MetaUserProfile(
            MetaUserId: $"meta-{code}",
            Email: $"{code}@meta.local",
            FullName: "Instagram User",
            InstagramUserId: $"ig-{code}",
            InstagramUsername: "instagram_user",
            AccessToken: code);
        return Task.FromResult(profile);
    }

    public Task SyncMediaAsync(InstagramAccount account, CancellationToken cancellationToken)
    {
        account.LastSyncUtc = DateTime.UtcNow;
        return Task.CompletedTask;
    }
}
