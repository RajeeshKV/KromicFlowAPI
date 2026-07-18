using KromicFlow.Application.Common;
using KromicFlow.Application.DTOs.Auth;
using MediatR;

namespace KromicFlow.Application.Features.Auth.Refresh;

public sealed record RefreshCommand(string RefreshToken, Guid SessionGuid) : IRequest<Result<AuthTokenDto>>;
