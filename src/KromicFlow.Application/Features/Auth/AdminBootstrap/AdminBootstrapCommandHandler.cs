using KromicFlow.Application.Abstractions;
using KromicFlow.Application.Common;
using KromicFlow.Application.DTOs.Auth;
using KromicFlow.Application.Options;
using KromicFlow.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace KromicFlow.Application.Features.Auth.AdminBootstrap;

internal sealed class AdminBootstrapCommandHandler(
    IKromicFlowDbContext db,
    IPasswordHasher passwordHasher,
    IAuditWriter auditWriter,
    IOptions<PlatformOptions> platformOptions) : IRequestHandler<AdminBootstrapCommand, Result<AdminProfileDto>>
{
    public async Task<Result<AdminProfileDto>> Handle(AdminBootstrapCommand request, CancellationToken cancellationToken)
    {
        if (await db.AdminUsers.AnyAsync(cancellationToken))
        {
            return Result<AdminProfileDto>.Failure("Admin user already exists.");
        }

        if (string.IsNullOrWhiteSpace(platformOptions.Value.AdminBootstrapKey) || request.BootstrapKey != platformOptions.Value.AdminBootstrapKey)
        {
            return Result<AdminProfileDto>.Failure("Invalid admin bootstrap key.");
        }

        var admin = new AdminUser { Username = request.Username, Email = request.Email, PasswordHash = passwordHasher.Hash(request.Password) };
        db.AdminUsers.Add(admin);
        await db.SaveChangesAsync(cancellationToken);
        await auditWriter.WriteAsync("AdminCreated", nameof(AdminUser), admin.Id.ToString(), null, admin.Id, null, cancellationToken);
        return Result<AdminProfileDto>.Success(new AdminProfileDto(admin.Id, admin.Username, admin.Email));
    }
}
