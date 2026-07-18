using KromicFlow.Application.Common;
using KromicFlow.Application.DTOs.Auth;
using MediatR;

namespace KromicFlow.Application.Features.Auth.AdminBootstrap;

public sealed record AdminBootstrapCommand(string BootstrapKey, string Username, string Email, string Password) : IRequest<Result<AdminProfileDto>>;
