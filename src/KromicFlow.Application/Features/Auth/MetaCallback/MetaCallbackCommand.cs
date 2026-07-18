using KromicFlow.Application.Common;
using KromicFlow.Application.DTOs.Auth;
using MediatR;

namespace KromicFlow.Application.Features.Auth.MetaCallback;

public sealed record MetaCallbackCommand(string Code, string State, string RedirectUri, string? DeviceName, string? Browser, string? OS, string? IPAddress) : IRequest<Result<LoginResponseDto>>;
