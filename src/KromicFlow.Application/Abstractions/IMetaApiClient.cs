using KromicFlow.Domain.Entities;

namespace KromicFlow.Application.Abstractions;

public interface IMetaApiClient
{
    Task<MetaUserProfile> ExchangeAuthorizationCodeAsync(string code, string redirectUri, CancellationToken cancellationToken);
    Task<string> ExchangeForLongLivedTokenAsync(string shortLivedToken, CancellationToken cancellationToken);
    Task<string> RefreshLongLivedTokenAsync(string longLivedToken, CancellationToken cancellationToken);
    Task SyncMediaAsync(InstagramAccount account, CancellationToken cancellationToken);
    Task<MetaInstagramBusinessAccount> RefreshInstagramAccountProfileAsync(string accessToken, string instagramAccountId, CancellationToken cancellationToken);
    Task<List<MetaInstagramMedia>> GetInstagramMediaAsync(string accessToken, string instagramUserId, CancellationToken cancellationToken);
    Task SubscribeToWebhooksAsync(string accessToken, string instagramUserId, CancellationToken cancellationToken);
    Task PostCommentReplyAsync(string accessToken, string commentId, string message, CancellationToken cancellationToken);
    Task SendPrivateReplyAsync(string accessToken, string igUserId, string commentId, string message, CancellationToken cancellationToken);
    // Keep for future use when a user has already initiated a conversation (24h window)
    Task SendDirectMessageAsync(string accessToken, string instagramUserId, string recipientIgsid, string message, CancellationToken cancellationToken);
}
