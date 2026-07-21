using KromicFlow.Application.Abstractions;
using KromicFlow.Application.Common;
using KromicFlow.Application.DTOs.Auth;
using KromicFlow.Application.Options;
using KromicFlow.Domain.Entities;
using KromicFlow.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace KromicFlow.Application.Features.Auth.MetaCallback;

internal sealed class MetaCallbackCommandHandler(
    IKromicFlowDbContext db,
    IMetaApiClient metaApiClient,
    IRefreshTokenService refreshTokenService,
    IJwtTokenService jwtTokenService,
    IAuditWriter auditWriter,
    IOAuthStateService oauthStateService,
    IDataProtectionService dataProtectionService,
    IOptions<PlatformOptions> platformOptions,
    IOptions<JwtOptions> jwtOptions,
    ILogger<MetaCallbackCommandHandler> logger) : IRequestHandler<MetaCallbackCommand, Result<LoginResponseDto>>
{
    public async Task<Result<LoginResponseDto>> Handle(MetaCallbackCommand request, CancellationToken cancellationToken)
    {
        if (!oauthStateService.ValidateState(request.State))
        {
            return Result<LoginResponseDto>.Failure("Invalid OAuth state. Possible CSRF attack.");
        }

        var profile = await metaApiClient.ExchangeAuthorizationCodeAsync(request.Code, request.RedirectUri, cancellationToken);
        var plan = await db.Plans.FirstOrDefaultAsync(x => x.Code == platformOptions.Value.DefaultPlanCode, cancellationToken)
            ?? await db.Plans.FirstAsync(x => x.IsDefault, cancellationToken);

        // Instagram account is the source of truth for user identity
        var firstIgAccount = profile.InstagramAccounts.FirstOrDefault();
        if (firstIgAccount is null)
        {
            logger.LogWarning("No Instagram accounts found in OAuth profile");
            return Result<LoginResponseDto>.Failure("No Instagram accounts associated with this account");
        }

        // Check if this Instagram account is already connected to a user
        var existingInstagramAccount = await db.InstagramAccounts
            .Include(x => x.User)
            .FirstOrDefaultAsync(x => x.InstagramUserId == firstIgAccount.InstagramAccountId, cancellationToken);

        Domain.Entities.User user;

        if (existingInstagramAccount is not null)
        {
            // Instagram account already exists - use the associated user (re-login)
            user = existingInstagramAccount.User;
            logger.LogInformation("User {UserId} re-authenticating with Instagram account {InstagramUserId}", 
                user.Id, firstIgAccount.InstagramAccountId);
        }
        else
        {
            // New Instagram account - create new user or find existing by email
            user = null;
            
            // Try to find user by email if email is provided
            if (!string.IsNullOrEmpty(profile.Email))
            {
                user = await db.Users.Include(x => x.Plan).FirstOrDefaultAsync(x => x.Email == profile.Email, cancellationToken);
                if (user is not null)
                {
                    logger.LogInformation("Found existing user {UserId} by email {Email}, connecting new Instagram account", 
                        user.Id, profile.Email);
                }
            }

            // Create new user if no existing user found
            if (user is null)
            {
                user = new Domain.Entities.User 
                { 
                    Email = profile.Email, 
                    FullName = profile.FullName, 
                    PlanId = plan.Id, 
                    Plan = plan 
                };
                db.Users.Add(user);
                db.TermsAcceptances.Add(new TermsAcceptance { User = user, TermsVersion = platformOptions.Value.TermsVersion });
                logger.LogInformation("Created new user for Instagram account {InstagramUserId}", firstIgAccount.InstagramAccountId);
            }
        }

        var loginBlocked = await db.UserRestrictions.AnyAsync(x => x.UserId == user.Id && x.LoginBlocked, cancellationToken);
        if (!user.IsActive || loginBlocked) 
            return Result<LoginResponseDto>.Failure("User login is restricted.");

        var encryptedToken = dataProtectionService.Protect(profile.AccessToken);
        var tokenExpiry = DateTime.UtcNow.AddDays(60);
        
        // Upsert all discovered Instagram accounts
        foreach (var igAccount in profile.InstagramAccounts)
        {
            var existingAccount = await db.InstagramAccounts
                .FirstOrDefaultAsync(x => x.InstagramUserId == igAccount.InstagramAccountId, cancellationToken);
            
            if (existingAccount is null)
            {
                // New account - mark as connected since user just authenticated
                var newAccount = new InstagramAccount
                {
                    User = user,
                    InstagramUserId = igAccount.InstagramAccountId,  // IG_ID — matches webhook entry.id
                    InstagramScopedId = igAccount.ScopedId,          // IGSID — app-scoped, for reference
                    FacebookPageId = igAccount.PageId,
                    Username = igAccount.Username,
                    DisplayName = igAccount.Username,
                    ProfilePicture = igAccount.ProfilePicture,
                    AccessTokenEncrypted = encryptedToken,
                    TokenExpiresUtc = tokenExpiry,
                    LastTokenRefreshUtc = DateTime.UtcNow,
                    TokenStatus = "active",
                    IsConnected = true,
                    ConnectedAtUtc = DateTime.UtcNow,
                    RefreshRequired = false
                };
                db.InstagramAccounts.Add(newAccount);
            }
            else
            {
                // Update existing account - re-connect since user just authenticated
                existingAccount.InstagramScopedId = igAccount.ScopedId;
                existingAccount.FacebookPageId = igAccount.PageId;
                existingAccount.Username = igAccount.Username;
                existingAccount.DisplayName = igAccount.Username;
                existingAccount.ProfilePicture = igAccount.ProfilePicture;
                existingAccount.AccessTokenEncrypted = encryptedToken;
                existingAccount.TokenExpiresUtc = tokenExpiry;
                existingAccount.LastTokenRefreshUtc = DateTime.UtcNow;
                existingAccount.TokenStatus = "active";
                existingAccount.RefreshRequired = false;
                existingAccount.UpdatedUtc = DateTime.UtcNow;
                existingAccount.IsConnected = true;
                existingAccount.ConnectedAtUtc = existingAccount.ConnectedAtUtc ?? DateTime.UtcNow;
                existingAccount.DisconnectedAtUtc = null;
            }
        }

        // Subscribe to webhooks for the Instagram account
        try
        {
            var decryptedToken = dataProtectionService.Unprotect(encryptedToken);
            await metaApiClient.SubscribeToWebhooksAsync(decryptedToken, profile.InstagramUserId, cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Failed to subscribe to webhooks for user {InstagramUserId}", profile.InstagramUserId);
        }

        var refreshToken = refreshTokenService.CreateToken();
        var session = new Session
        {
            User = user,
            RefreshTokenHash = refreshTokenService.Hash(refreshToken),
            DeviceName = request.DeviceName,
            Browser = request.Browser,
            OS = request.OS,
            IPAddress = request.IPAddress,
            ExpiresUtc = DateTime.UtcNow.AddDays(jwtOptions.Value.RefreshTokenDays)
        };

        db.Sessions.Add(session);
        db.UserActivities.Add(new UserActivity { User = user, Type = ActivityType.Login, Description = "Meta OAuth login" });
        await db.SaveChangesAsync(cancellationToken);
        await auditWriter.WriteAsync("Login", nameof(Domain.Entities.User), user.Id.ToString(), user.Id, null, null, cancellationToken);

        var tokens = jwtTokenService.CreateUserToken(user, session) with { RefreshToken = refreshToken };
        var dto = new UserProfileDto(user.Id, user.Email, user.FullName, user.Role.ToString(), plan.Code, user.IsActive, user.EmailVerified, user.MarketingEmailEnabled, user.MarketingPushEnabled);
        return Result<LoginResponseDto>.Success(new LoginResponseDto(tokens, dto));
    }
}

