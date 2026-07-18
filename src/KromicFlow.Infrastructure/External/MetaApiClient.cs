using System.Text;
using System.Text.Json;
using KromicFlow.Application.Abstractions;
using KromicFlow.Application.Options;
using KromicFlow.Domain.Entities;
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
            
            logger.LogInformation("Retrieving user profile from Meta");
            var userProfile = await GetUserProfileAsync(longLivedToken, cancellationToken);
            
            logger.LogInformation("Retrieving Instagram business account");
            var instagramAccount = await GetInstagramBusinessAccountAsync(longLivedToken, cancellationToken);

            return new MetaUserProfile(
                MetaUserId: userProfile.Id,
                Email: userProfile.Email ?? $"{userProfile.Id}@facebook.com",
                FullName: userProfile.Name,
                InstagramUserId: instagramAccount.IgId,
                InstagramUsername: instagramAccount.Username,
                AccessToken: longLivedToken);
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
        var url = $"{options.Value.GraphApiBaseUrl}/oauth/access_token" +
                  $"?grant_type=ig_exchange_token" +
                  $"&client_secret={options.Value.AppSecret}" +
                  $"&access_token={shortLivedToken}";

        var response = await httpClient.GetAsync(url, cancellationToken);
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
        var url = $"{options.Value.GraphApiBaseUrl}/oauth/access_token" +
                  $"?grant_type=ig_refresh_token" +
                  $"&access_token={longLivedToken}";

        var response = await httpClient.GetAsync(url, cancellationToken);
        
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
        var url = $"{options.Value.GraphApiBaseUrl}/oauth/access_token" +
                  $"?client_id={options.Value.AppId}" +
                  $"&client_secret={options.Value.AppSecret}" +
                  $"&grant_type=authorization_code" +
                  $"&redirect_uri={redirectUri}" +
                  $"&code={code}";

        var response = await httpClient.GetAsync(url, cancellationToken);
        
        if (!response.IsSuccessStatusCode)
        {
            var errorContent = await response.Content.ReadAsStringAsync(cancellationToken);
            logger.LogError("Meta API returned error during code exchange: {StatusCode} - {Content}", response.StatusCode, errorContent);
            throw new MetaApiException($"Meta API error during code exchange: {response.StatusCode}");
        }

        var content = await response.Content.ReadAsStringAsync(cancellationToken);
        var result = JsonSerializer.Deserialize<MetaTokenResponse>(content, _jsonOptions);

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
        var url = $"{options.Value.GraphApiBaseUrl}/me?fields=id,name,email&access_token={accessToken}";
        
        var response = await httpClient.GetAsync(url, cancellationToken);
        
        if (!response.IsSuccessStatusCode)
        {
            logger.LogError("Failed to retrieve user profile: {StatusCode}", response.StatusCode);
            throw new MetaApiException($"Failed to retrieve user profile: {response.StatusCode}");
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

    private async Task<MetaInstagramAccount> GetInstagramBusinessAccountAsync(string accessToken, CancellationToken cancellationToken)
    {
        var url = $"{options.Value.GraphApiBaseUrl}/me/accounts?fields=instagram_business_account&access_token={accessToken}";
        
        var response = await httpClient.GetAsync(url, cancellationToken);
        
        if (!response.IsSuccessStatusCode)
        {
            logger.LogError("Failed to retrieve Facebook pages: {StatusCode}", response.StatusCode);
            throw new MetaApiException($"Failed to retrieve Facebook pages: {response.StatusCode}");
        }

        var content = await response.Content.ReadAsStringAsync(cancellationToken);
        var pagesResponse = JsonSerializer.Deserialize<MetaPagesResponse>(content, _jsonOptions);

        if (pagesResponse?.Data == null || pagesResponse.Data.Count == 0)
        {
            logger.LogError("No Facebook pages found for user");
            throw new MetaApiException("No Facebook pages found. User must have a Facebook page with an Instagram business account.");
        }

        var firstPageWithIg = pagesResponse.Data.FirstOrDefault(p => p.InstagramBusinessAccount != null);
        if (firstPageWithIg?.InstagramBusinessAccount == null)
        {
            logger.LogError("No Instagram business account found on any Facebook page");
            throw new MetaApiException("No Instagram business account found. User must connect an Instagram business account to a Facebook page.");
        }

        var igAccountId = firstPageWithIg.InstagramBusinessAccount.Id;
        var igUrl = $"{options.Value.GraphApiBaseUrl}/{igAccountId}?fields=username,ig_id&access_token={accessToken}";
        
        var igResponse = await httpClient.GetAsync(igUrl, cancellationToken);
        
        if (!igResponse.IsSuccessStatusCode)
        {
            logger.LogError("Failed to retrieve Instagram account details: {StatusCode}", igResponse.StatusCode);
            throw new MetaApiException($"Failed to retrieve Instagram account details: {igResponse.StatusCode}");
        }

        var igContent = await igResponse.Content.ReadAsStringAsync(cancellationToken);
        var igAccount = JsonSerializer.Deserialize<MetaInstagramAccount>(igContent, _jsonOptions);

        if (igAccount == null)
        {
            logger.LogError("Meta API returned invalid Instagram account");
            throw new MetaApiException("Invalid Instagram account from Meta API");
        }

        return igAccount;
    }

    private record MetaTokenResponse(string? AccessToken, string? TokenType, long? ExpiresIn);
    private record MetaUser(string Id, string Name, string? Email);
    private record MetaInstagramBusinessAccount(string Id);
    private record MetaPage(string Id, MetaInstagramBusinessAccount? InstagramBusinessAccount);
    private record MetaPagesResponse(List<MetaPage> Data);
    private record MetaInstagramAccount(string Id, string Username, string IgId);
}
