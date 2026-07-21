using KromicFlow.Application.Abstractions;
using KromicFlow.Application.DTOs.Auth;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace KromicFlow.Application.Features.User.GetUserProfile;

internal sealed class GetUserProfileQueryHandler(
    IKromicFlowDbContext db,
    ILogger<GetUserProfileQueryHandler> logger) : IRequestHandler<GetUserProfileQuery, UserProfileDto>
{
    public async Task<UserProfileDto> Handle(GetUserProfileQuery request, CancellationToken cancellationToken)
    {
        logger.LogInformation("Fetching user profile for user {UserId}", request.UserId);

        var user = await db.Users
            .Include(x => x.Plan)
            .FirstOrDefaultAsync(x => x.Id == request.UserId, cancellationToken);

        if (user == null)
        {
            logger.LogWarning("User not found: {UserId}", request.UserId);
            throw new InvalidOperationException("User not found");
        }

        var dto = new UserProfileDto(
            user.Id,
            user.Email ?? string.Empty,
            user.FullName,
            user.Role.ToString(),
            user.Plan.Code,
            user.IsActive,
            user.EmailVerified,
            user.MarketingEmailEnabled,
            user.MarketingPushEnabled);

        logger.LogInformation("User profile fetched successfully for {UserId}", request.UserId);
        return dto;
    }
}
