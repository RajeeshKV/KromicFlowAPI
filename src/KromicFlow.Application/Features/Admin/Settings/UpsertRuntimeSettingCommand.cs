using KromicFlow.Application.Common;
using KromicFlow.Application.DTOs.Admin;
using MediatR;

namespace KromicFlow.Application.Features.Admin.Settings;

public sealed record UpsertRuntimeSettingCommand(Guid AdminId, string Key, string Value, bool IsSecret, string? Description) : IRequest<Result<RuntimeSettingDto>>;
