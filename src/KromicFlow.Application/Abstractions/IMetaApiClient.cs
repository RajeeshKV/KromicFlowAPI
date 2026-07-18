using KromicFlow.Domain.Entities;

namespace KromicFlow.Application.Abstractions;

public interface IMetaApiClient
{
    Task<MetaUserProfile> ExchangeAuthorizationCodeAsync(string code, string redirectUri, CancellationToken cancellationToken);
    Task<string> ExchangeForLongLivedTokenAsync(string shortLivedToken, CancellationToken cancellationToken);
    Task<string> RefreshLongLivedTokenAsync(string longLivedToken, CancellationToken cancellationToken);
    Task SyncMediaAsync(InstagramAccount account, CancellationToken cancellationToken);
}
