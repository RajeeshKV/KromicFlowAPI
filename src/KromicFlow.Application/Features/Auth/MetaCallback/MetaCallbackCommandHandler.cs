using KromicFlow.Application.Abstractions;
using KromicFlow.Application.Common;
using KromicFlow.Application.DTOs.Auth;
using KromicFlow.Application.Options;
using KromicFlow.Domain.Entities;
using KromicFlow.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;
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
    IOptions<JwtOptions> jwtOptions) : IRequestHandler<MetaCallbackCommand, Result<LoginResponseDto>>
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

        var user = await db.Users.Include(x => x.Plan).FirstOrDefaultAsync(x => x.Email == profile.Email, cancellationToken);
        if (user is null)
        {
            user = new Domain.Entities.User { Email = profile.Email, FullName = profile.FullName, PlanId = plan.Id, Plan = plan };
            db.Users.Add(user);
            db.TermsAcceptances.Add(new TermsAcceptance { User = user, TermsVersion = platformOptions.Value.TermsVersion });
        }

        var loginBlocked = await db.UserRestrictions.AnyAsync(x => x.UserId == user.Id && x.LoginBlocked, cancellationToken);
        if (!user.IsActive || loginBlocked) return Result<LoginResponseDto>.Failure("User login is restricted.");

        var encryptedToken = dataProtectionService.Protect(profile.AccessToken);
        var tokenExpiry = DateTime.UtcNow.AddDays(60);
        
        // Upsert all discovered Instagram accounts
        foreach (var igAccount in profile.InstagramAccounts)
        {
            var existingAccount = await db.InstagramAccounts
                .FirstOrDefaultAsync(x => x.InstagramUserId == igAccount.InstagramAccountId, cancellationToken);
            
            if (existingAccount is null)
            {
                // New account - mark as disconnected by default
                var newAccount = new InstagramAccount
                {
                    User = user,
                    InstagramUserId = igAccount.InstagramAccountId,
                    FacebookPageId = igAccount.PageId,
                    Username = igAccount.Username,
                    DisplayName = igAccount.Username,
                    ProfilePicture = igAccount.ProfilePicture,
                    AccessTokenEncrypted = encryptedToken,
                    TokenExpiresUtc = tokenExpiry,
                    LastTokenRefreshUtc = DateTime.UtcNow,
                    TokenStatus = "active",
                    IsConnected = false, // Newly discovered accounts are disconnected by default
                    RefreshRequired = false
                };
                db.InstagramAccounts.Add(newAccount);
            }
            else
            {
                // Update existing account
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
                // Preserve existing connection state - don't auto-connect
            }
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
        await auditWriter.WriteAsync("Login", nameof(User), user.Id.ToString(), user.Id, null, null, cancellationToken);

        var tokens = jwtTokenService.CreateUserToken(user, session) with { RefreshToken = refreshToken };
        var dto = new UserProfileDto(user.Id, user.Email, user.FullName, user.Role.ToString(), plan.Code, user.IsActive, user.MarketingEmailEnabled, user.MarketingPushEnabled);
        return Result<LoginResponseDto>.Success(new LoginResponseDto(tokens, dto));
    }
}

