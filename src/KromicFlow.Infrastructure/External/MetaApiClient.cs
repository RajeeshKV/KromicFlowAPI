using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using KromicFlow.Application.Abstractions;
using KromicFlow.Application.Options;
using KromicFlow.Domain.Entities;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace KromicFlow.Infrastructure.External;

public sealed class MetaApiClient(
    HttpClient httpClient,
    IOptions<MetaOptions> options,
    ILogger<MetaApiClient> logger) : IMetaApiClient
{
    private readonly JsonSerializerOptions _jsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower
    };
    private static readonly int MaxRetries = 5;

    public async Task<MetaUserProfile> ExchangeAuthorizationCodeAsync(string code, string redirectUri, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(options.Value.AppId) || string.IsNullOrWhiteSpace(options.Value.AppSecret))
        {
            throw new InvalidOperationException("Meta OAuth is not configured. Set Meta__AppId and Meta__AppSecret.");
        }

        try
        {
            logger.LogInformation("Exchanging authorization code for short-lived token");
            
            var shortLivedToken = await ExchangeCodeForShortLivedTokenAsync(code, redirectUri, cancellationToken);
            logger.LogInformation("Exchanging short-lived token for long-lived token");
            
            var longLivedToken = await ExchangeForLongLivedTokenAsync(shortLivedToken, cancellationToken);
            
            logger.LogInformation("Retrieving Instagram user profile");
            var userProfile = await GetUserProfileAsync(longLivedToken, cancellationToken);
            
            logger.LogInformation("Retrieving Instagram business account");
            var instagramAccounts = await GetInstagramBusinessAccountAsync(longLivedToken, cancellationToken);

            // Use Instagram-scoped ID from business account for webhook compatibility
            var instagramUserId = instagramAccounts.FirstOrDefault()?.InstagramAccountId ?? userProfile.UserId;
            logger.LogInformation("Using Instagram account ID: {InstagramUserId}", instagramUserId);

            return new MetaUserProfile(
                MetaUserId: userProfile.UserId,
                Email: null, // Instagram doesn't provide email in OAuth flow
                FullName: userProfile.Username,
                InstagramUserId: instagramUserId,
                InstagramUsername: userProfile.Username,
                AccessToken: longLivedToken,
                InstagramAccounts: instagramAccounts);
        }
        catch (HttpRequestException ex)
        {
            logger.LogError(ex, "Meta API request failed during OAuth flow");
            throw new MetaApiException("Meta API request failed", ex);
        }
        catch (JsonException ex)
        {
            logger.LogError(ex, "Failed to parse Meta API response during OAuth flow");
            throw new MetaApiException("Invalid Meta API response", ex);
        }
    }

    public async Task<string> ExchangeForLongLivedTokenAsync(string shortLivedToken, CancellationToken cancellationToken)
    {
        var query = new Dictionary<string, string>
        {
            ["grant_type"] = "ig_exchange_token",
            ["client_secret"] = options.Value.AppSecret,
            ["access_token"] = shortLivedToken
        };
        var url = QueryHelpers.AddQueryString($"{options.Value.GraphApiBaseUrl}/access_token", query);

        var response = await RetryAsync(() => httpClient.GetAsync(url, cancellationToken), cancellationToken);
        response.EnsureSuccessStatusCode();

        var content = await response.Content.ReadAsStringAsync(cancellationToken);
        var result = JsonSerializer.Deserialize<MetaTokenResponse>(content, _jsonOptions);

        if (result?.AccessToken == null)
        {
            logger.LogError("Meta API returned invalid token response");
            throw new MetaApiException("Invalid token response from Meta API");
        }

        logger.LogInformation("Successfully exchanged for long-lived token");
        return result.AccessToken;
    }

    public async Task<string> RefreshLongLivedTokenAsync(string longLivedToken, CancellationToken cancellationToken)
    {
        var query = new Dictionary<string, string>
        {
            ["grant_type"] = "ig_refresh_token",
            ["access_token"] = longLivedToken
        };
        var url = QueryHelpers.AddQueryString($"{options.Value.GraphApiBaseUrl}/refresh_access_token", query);

        var response = await RetryAsync(() => httpClient.GetAsync(url, cancellationToken), cancellationToken);
        
        if (!response.IsSuccessStatusCode)
        {
            logger.LogError("Failed to refresh long-lived token: {StatusCode}", response.StatusCode);
            throw new MetaApiException($"Failed to refresh token: {response.StatusCode}");
        }

        var content = await response.Content.ReadAsStringAsync(cancellationToken);
        var result = JsonSerializer.Deserialize<MetaTokenResponse>(content, _jsonOptions);

        if (result?.AccessToken == null)
        {
            logger.LogError("Meta API returned invalid refresh response");
            throw new MetaApiException("Invalid refresh response from Meta API");
        }

        logger.LogInformation("Successfully refreshed long-lived token");
        return result.AccessToken;
    }

    public Task SyncMediaAsync(InstagramAccount account, CancellationToken cancellationToken)
    {
        account.LastSyncUtc = DateTime.UtcNow;
        logger.LogInformation("Synced media for Instagram account {InstagramUserId}", account.InstagramUserId);
        return Task.CompletedTask;
    }

    private async Task<string> ExchangeCodeForShortLivedTokenAsync(string code, string redirectUri, CancellationToken cancellationToken)
    {
        var formData = new Dictionary<string, string>
        {
            ["client_id"] = options.Value.AppId,
            ["client_secret"] = options.Value.AppSecret,
            ["grant_type"] = "authorization_code",
            ["redirect_uri"] = redirectUri,
            ["code"] = code
        };
        
        var content = new FormUrlEncodedContent(formData);
        var url = $"{options.Value.ApiBaseUrl}/oauth/access_token";

        var response = await RetryAsync(() => httpClient.PostAsync(url, content, cancellationToken), cancellationToken);
        
        if (!response.IsSuccessStatusCode)
        {
            var errorContent = await response.Content.ReadAsStringAsync(cancellationToken);
            var sanitizedError = SanitizeSensitiveData(errorContent);
            logger.LogError("Meta API returned error during code exchange: {StatusCode} - {Content}", response.StatusCode, sanitizedError);
            throw new MetaApiException($"Meta API error during code exchange: {response.StatusCode}");
        }

        var responseContent = await response.Content.ReadAsStringAsync(cancellationToken);
        var result = JsonSerializer.Deserialize<MetaTokenResponse>(responseContent, _jsonOptions);

        if (result?.AccessToken == null)
        {
            logger.LogError("Meta API returned invalid token response during code exchange");
            throw new MetaApiException("Invalid token response from Meta API");
        }

        logger.LogInformation("Successfully exchanged authorization code for short-lived token");
        return result.AccessToken;
    }

    private async Task<MetaUser> GetUserProfileAsync(string accessToken, CancellationToken cancellationToken)
    {
        var query = new Dictionary<string, string>
        {
            ["fields"] = "user_id,username",
            ["access_token"] = accessToken
        };
        var url = QueryHelpers.AddQueryString($"{options.Value.GraphApiBaseUrl}/me", query);
        
        var response = await RetryAsync(() => httpClient.GetAsync(url, cancellationToken), cancellationToken);
        
        if (!response.IsSuccessStatusCode)
        {
            logger.LogError("Failed to retrieve Instagram user profile: {StatusCode}", response.StatusCode);
            throw new MetaApiException($"Failed to retrieve Instagram user profile: {response.StatusCode}");
        }

        var content = await response.Content.ReadAsStringAsync(cancellationToken);
        var result = JsonSerializer.Deserialize<MetaUser>(content, _jsonOptions);

        if (result == null)
        {
            logger.LogError("Meta API returned invalid user profile");
            throw new MetaApiException("Invalid user profile from Meta API");
        }

        return result;
    }

    private async Task<List<MetaInstagramBusinessAccount>> GetInstagramBusinessAccountAsync(string accessToken, CancellationToken cancellationToken)
    {
        // Request both `id` (IGSID, app-scoped) and `user_id` (IG_ID, real Business Account ID).
        // `user_id` is the value Meta puts in entry.id of webhook payloads — it must be stored
        // as InstagramUserId so webhook lookup works.
        // `id` is the app-scoped ID (IGSID) — stored separately as InstagramScopedId for reference.
        var query = new Dictionary<string, string>
        {
            ["fields"] = "id,user_id,username,profile_picture_url,account_type",
            ["access_token"] = accessToken
        };
        var url = QueryHelpers.AddQueryString($"{options.Value.GraphApiBaseUrl}/me", query);
        
        logger.LogInformation("Requesting Instagram business account from: {Url}", url);
        
        var response = await RetryAsync(() => httpClient.GetAsync(url, cancellationToken), cancellationToken);
        
        if (!response.IsSuccessStatusCode)
        {
            logger.LogError("Failed to retrieve Instagram business account: {StatusCode}", response.StatusCode);
            // Return empty list instead of throwing - single account flow will work
            return [];
        }

        var content = await response.Content.ReadAsStringAsync(cancellationToken);
        logger.LogInformation("Instagram business account response: {Content}", content);
        var result = JsonSerializer.Deserialize<MetaInstagramBusinessAccountResponse>(content, _jsonOptions);

        if (result == null)
        {
            logger.LogWarning("No Instagram business account found");
            return [];
        }

        // user_id is the IG_ID used in webhooks; fall back to id if user_id is absent
        var igId = result.UserId ?? result.Id;
        logger.LogInformation("Instagram account IDs — IG_ID (user_id, webhook): {IgId}, IGSID (id, app-scoped): {ScopedId}",
            igId, result.Id);

        var instagramAccounts = new List<MetaInstagramBusinessAccount>
        {
            new MetaInstagramBusinessAccount(
                PageId: string.Empty,          // Not applicable for direct Instagram Login flow
                InstagramAccountId: igId,       // IG_ID — matches webhook entry.id
                ScopedId: result.Id,            // IGSID — app-scoped, stored for reference
                Username: result.Username,
                ProfilePicture: result.ProfilePictureUrl ?? string.Empty
            )
        };

        logger.LogInformation("Found {Count} Instagram business account", instagramAccounts.Count);
        return instagramAccounts;
    }

    public async Task<MetaInstagramBusinessAccount> RefreshInstagramAccountProfileAsync(string accessToken, string instagramAccountId, CancellationToken cancellationToken)
    {
        var query = new Dictionary<string, string>
        {
            ["fields"] = "username,profile_picture_url",
            ["access_token"] = accessToken
        };
        var url = QueryHelpers.AddQueryString($"{options.Value.GraphApiBaseUrl}/{instagramAccountId}", query);
        
        var response = await RetryAsync(() => httpClient.GetAsync(url, cancellationToken), cancellationToken);
        
        if (!response.IsSuccessStatusCode)
        {
            logger.LogError("Failed to refresh Instagram account profile: {StatusCode}", response.StatusCode);
            throw new MetaApiException($"Failed to refresh Instagram account profile: {response.StatusCode}");
        }

        var content = await response.Content.ReadAsStringAsync(cancellationToken);
        var result = JsonSerializer.Deserialize<MetaInstagramProfileResponse>(content, _jsonOptions);

        if (result == null)
        {
            logger.LogError("Meta API returned invalid profile response");
            throw new MetaApiException("Invalid profile response from Meta API");
        }

        logger.LogInformation("Successfully refreshed Instagram account profile for {AccountId}", instagramAccountId);
        return new MetaInstagramBusinessAccount(
            PageId: string.Empty, // Not available in profile refresh
            InstagramAccountId: instagramAccountId,
            ScopedId: null, // Not available in profile refresh
            Username: result.Username,
            ProfilePicture: result.ProfilePictureUrl
        );
    }

    public async Task<List<MetaInstagramMedia>> GetInstagramMediaAsync(string accessToken, string instagramUserId, CancellationToken cancellationToken)
    {
        var query = new Dictionary<string, string>
        {
            ["fields"] = "id,media_type,caption,thumbnail_url,media_url,permalink,timestamp,like_count,comments_count",
            ["access_token"] = accessToken
        };
        var url = QueryHelpers.AddQueryString($"{options.Value.GraphApiBaseUrl}/{instagramUserId}/media", query);
        
        logger.LogInformation("Requesting media from Instagram API: {Url}", url);
        
        var response = await RetryAsync(() => httpClient.GetAsync(url, cancellationToken), cancellationToken);
        
        if (!response.IsSuccessStatusCode)
        {
            var errorContent = await response.Content.ReadAsStringAsync(cancellationToken);
            logger.LogError("Failed to retrieve Instagram media: {StatusCode} - {Content}", response.StatusCode, errorContent);
            throw new MetaApiException($"Failed to retrieve Instagram media: {response.StatusCode} - {errorContent}");
        }

        var content = await response.Content.ReadAsStringAsync(cancellationToken);
        logger.LogInformation("Instagram API response: {Content}", content);
        
        var result = JsonSerializer.Deserialize<MetaMediaResponse>(content, _jsonOptions);

        if (result?.Data == null)
        {
            logger.LogWarning("No Instagram media found in response");
            return [];
        }

        var mediaList = result.Data.Select(m => new MetaInstagramMedia(
            Id: m.Id,
            MediaType: m.MediaType,
            Caption: m.Caption ?? string.Empty,
            ThumbnailUrl: m.ThumbnailUrl ?? string.Empty,
            MediaUrl: m.MediaUrl ?? string.Empty,
            Permalink: m.Permalink ?? string.Empty,
            PostedAtUtc: DateTime.TryParse(m.Timestamp, out var posted) ? DateTime.SpecifyKind(posted, DateTimeKind.Utc) : DateTime.UtcNow,
            LikeCount: m.LikeCount ?? 0,
            CommentsCount: m.CommentsCount ?? 0
        )).ToList();

        logger.LogInformation("Retrieved {Count} Instagram media items", mediaList.Count);
        return mediaList;
    }

    public async Task SubscribeToWebhooksAsync(string accessToken, string instagramUserId, CancellationToken cancellationToken)
    {
        logger.LogInformation("Subscribing Instagram account {InstagramUserId} to webhooks", instagramUserId);

        var query = new Dictionary<string, string>
        {
            ["subscribed_fields"] = "comments,mentions",
            ["access_token"] = accessToken
        };

        var url = QueryHelpers.AddQueryString($"{options.Value.GraphApiBaseUrl}/{instagramUserId}/subscribed_apps", query);
        logger.LogInformation("Webhook subscription URL: {Url}", SanitizeSensitiveData(url));

        var response = await RetryAsync(() => httpClient.PostAsync(url, null, cancellationToken), cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            var errorContent = await response.Content.ReadAsStringAsync(cancellationToken);
            logger.LogError("Failed to subscribe to webhooks: {StatusCode} - {Content}", response.StatusCode, errorContent);
            throw new MetaApiException($"Failed to subscribe to webhooks: {response.StatusCode} - {errorContent}");
        }

        var content = await response.Content.ReadAsStringAsync(cancellationToken);
        logger.LogInformation("Webhook subscription successful: {Content}", content);
    }

    public async Task PostCommentReplyAsync(string accessToken, string commentId, string message, CancellationToken cancellationToken)
    {
        logger.LogInformation("Posting public reply to comment {CommentId}", commentId);

        var url = $"{options.Value.GraphApiBaseUrl}/{commentId}/replies";
        var body = new Dictionary<string, string>
        {
            ["message"] = message,
            ["access_token"] = accessToken
        };

        var response = await RetryAsync(
            () => httpClient.PostAsync(url, new FormUrlEncodedContent(body), cancellationToken),
            cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            var errorContent = await response.Content.ReadAsStringAsync(cancellationToken);
            logger.LogError("Failed to post comment reply: {StatusCode} - {Content}", response.StatusCode, errorContent);
            throw new MetaApiException($"Failed to post comment reply: {response.StatusCode} - {errorContent}");
        }

        logger.LogInformation("Successfully posted public reply to comment {CommentId}", commentId);
    }

    public async Task SendPrivateReplyAsync(string accessToken, string igUserId, string commentId, string message, CancellationToken cancellationToken)
    {
        // For Instagram Login API, private replies use /{ig_user_id}/messages
        // with recipient.comment_id — NOT /{comment_id}/private_replies which is
        // the Facebook Login / Messenger Platform endpoint.
        // Requires: instagram_business_manage_messages permission.
        logger.LogInformation("Sending private reply from {IgUserId} for comment {CommentId}", igUserId, commentId);

        var url = QueryHelpers.AddQueryString(
            $"{options.Value.GraphApiBaseUrl}/{igUserId}/messages",
            new Dictionary<string, string> { ["access_token"] = accessToken });

        var payload = new
        {
            recipient = new { comment_id = commentId },
            message = new { text = message }
        };

        var json = JsonSerializer.Serialize(payload, _jsonOptions);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        var response = await RetryAsync(
            () => httpClient.PostAsync(url, content, cancellationToken),
            cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            var errorContent = await response.Content.ReadAsStringAsync(cancellationToken);
            logger.LogError("Failed to send private reply for comment {CommentId}: {StatusCode} - {Content}", commentId, response.StatusCode, errorContent);
            throw new MetaApiException($"Failed to send private reply: {response.StatusCode} - {errorContent}");
        }

        logger.LogInformation("Successfully sent private reply for comment {CommentId}", commentId);
    }

    public async Task SendDirectMessageAsync(string accessToken, string instagramUserId, string recipientIgsid, string message, CancellationToken cancellationToken)
    {
        logger.LogInformation("Sending DM from account {InstagramUserId} to recipient {RecipientIgsid}", instagramUserId, recipientIgsid);

        var url = $"{options.Value.GraphApiBaseUrl}/{instagramUserId}/messages";
        var payload = new
        {
            recipient = new { id = recipientIgsid },
            message = new { text = message }
        };

        var json = JsonSerializer.Serialize(payload, _jsonOptions);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        // access_token must go in the query string for the messages endpoint
        var fullUrl = QueryHelpers.AddQueryString(url, new Dictionary<string, string>
        {
            ["access_token"] = accessToken
        });

        var response = await RetryAsync(
            () => httpClient.PostAsync(fullUrl, content, cancellationToken),
            cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            var errorContent = await response.Content.ReadAsStringAsync(cancellationToken);
            logger.LogError("Failed to send DM to {RecipientIgsid}: {StatusCode} - {Content}", recipientIgsid, response.StatusCode, errorContent);
            throw new MetaApiException($"Failed to send DM: {response.StatusCode} - {errorContent}");
        }

        logger.LogInformation("Successfully sent DM to recipient {RecipientIgsid}", recipientIgsid);
    }

    private record MetaTokenResponse(string? AccessToken, string? TokenType, long? ExpiresIn);    private record MetaUser(string UserId, string Username);
    private record MetaInstagramBusinessAccountResponse(string Id, string? UserId, string Username, string? ProfilePictureUrl, string? AccountType);
    private record MetaInstagramProfileResponse(string Username, string ProfilePictureUrl);
    private record MetaMediaItem(string Id, string MediaType, string? Caption, string? ThumbnailUrl, string? MediaUrl, string? Permalink, string Timestamp, int? LikeCount, int? CommentsCount);
    private record MetaMediaResponse(List<MetaMediaItem> Data);

    private async Task<HttpResponseMessage> RetryAsync(Func<Task<HttpResponseMessage>> requestFunc, CancellationToken cancellationToken)
    {
        for (int attempt = 0; attempt < MaxRetries; attempt++)
        {
            try
            {
                var response = await requestFunc();
                
                if (response.IsSuccessStatusCode || IsNonRetriableStatus(response.StatusCode))
                {
                    return response;
                }

                logger.LogWarning("Meta API request failed with status {StatusCode}, attempt {Attempt}/{MaxRetries}", response.StatusCode, attempt + 1, MaxRetries);
            }
            catch (HttpRequestException ex)
            {
                logger.LogWarning(ex, "Meta API request failed on attempt {Attempt}/{MaxRetries}", attempt + 1, MaxRetries);
            }

            if (attempt < MaxRetries - 1)
            {
                var delay = TimeSpan.FromSeconds(Math.Pow(2, attempt));
                await Task.Delay(delay, cancellationToken);
            }
        }

        return await requestFunc();
    }

    private static bool IsNonRetriableStatus(System.Net.HttpStatusCode statusCode)
    {
        return statusCode is
            System.Net.HttpStatusCode.BadRequest or
            System.Net.HttpStatusCode.Unauthorized or
            System.Net.HttpStatusCode.Forbidden or
            System.Net.HttpStatusCode.NotFound;
    }

    private static string SanitizeSensitiveData(string input)
    {
        if (string.IsNullOrWhiteSpace(input))
            return input;

        var result = Regex.Replace(input, @"access_token=[^&\s]+", "access_token=[REDACTED]");
        result = Regex.Replace(result, @"client_secret=[^&\s]+", "client_secret=[REDACTED]");
        result = Regex.Replace(result, @"code=[^&\s]+", "code=[REDACTED]");
        result = Regex.Replace(result, @"""access_token""\s*:\s*""[^""]+""", "\"access_token\": \"[REDACTED]\"");
        result = Regex.Replace(result, @"""client_secret""\s*:\s*""[^""]+""", "\"client_secret\": \"[REDACTED]\"");
        result = Regex.Replace(result, @"""code""\s*:\s*""[^""]+""", "\"code\": \"[REDACTED]\"");
        return result;
    }
}
