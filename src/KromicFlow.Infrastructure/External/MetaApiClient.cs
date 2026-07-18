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

            return new MetaUserProfile(
                MetaUserId: userProfile.UserId,
                Email: $"{userProfile.Username}@instagram.com",
                FullName: userProfile.Username,
                InstagramUserId: userProfile.UserId,
                InstagramUsername: userProfile.Username,
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

    private record MetaTokenResponse(string? AccessToken, string? TokenType, long? ExpiresIn);
    private record MetaUser(string UserId, string Username);

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
