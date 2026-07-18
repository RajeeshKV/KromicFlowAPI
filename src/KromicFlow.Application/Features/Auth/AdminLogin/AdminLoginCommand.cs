using KromicFlow.Application.Common;
using KromicFlow.Application.DTOs.Auth;
using MediatR;

namespace KromicFlow.Application.Features.Auth.AdminLogin;

public sealed record AdminLoginCommand(string Username, string Password, string? DeviceName, string? Browser, string? OS, string? IPAddress) : IRequest<Result<AdminLoginResponseDto>>;
