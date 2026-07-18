using KromicFlow.Application.Abstractions;
using KromicFlow.Application.Common;
using KromicFlow.Application.DTOs.Admin;
using KromicFlow.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace KromicFlow.Application.Features.Admin.Settings;

internal sealed class UpsertRuntimeSettingCommandHandler(IKromicFlowDbContext db, IAuditWriter auditWriter) : IRequestHandler<UpsertRuntimeSettingCommand, Result<RuntimeSettingDto>>
{
    public async Task<Result<RuntimeSettingDto>> Handle(UpsertRuntimeSettingCommand request, CancellationToken cancellationToken)
    {
        var setting = await db.RuntimeSettings.FirstOrDefaultAsync(x => x.Key == request.Key, cancellationToken);
        if (setting is null)
        {
            setting = new RuntimeSetting { Key = request.Key };
            db.RuntimeSettings.Add(setting);
        }
        setting.Value = request.Value;
        setting.IsSecret = request.IsSecret;
        setting.Description = request.Description;
        await db.SaveChangesAsync(cancellationToken);
        await auditWriter.WriteAsync("RuntimeSettingUpserted", nameof(RuntimeSetting), setting.Id.ToString(), null, request.AdminId, null, cancellationToken);
        return Result<RuntimeSettingDto>.Success(new RuntimeSettingDto(setting.Key, setting.IsSecret ? "***" : setting.Value, setting.IsSecret, setting.Description));
    }
}
