using KromicFlow.Domain.Entities;

namespace KromicFlow.Application.Abstractions;

public interface IMetaApiClient
{
    Task<MetaUserProfile> ExchangeAuthorizationCodeAsync(string code, string redirectUri, CancellationToken cancellationToken);
    Task SyncMediaAsync(InstagramAccount account, CancellationToken cancellationToken);
}
