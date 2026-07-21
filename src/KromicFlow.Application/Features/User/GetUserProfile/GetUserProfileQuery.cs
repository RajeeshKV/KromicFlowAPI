using KromicFlow.Application.DTOs.Auth;
using MediatR;

namespace KromicFlow.Application.Features.User.GetUserProfile;

public sealed record GetUserProfileQuery(Guid UserId) : IRequest<UserProfileDto>;
