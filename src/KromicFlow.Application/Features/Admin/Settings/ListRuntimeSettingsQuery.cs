using KromicFlow.Application.Common;
using KromicFlow.Application.DTOs.Admin;
using MediatR;

namespace KromicFlow.Application.Features.Admin.Settings;

public sealed record ListRuntimeSettingsQuery(int Page = 1, int PageSize = 50) : IRequest<PagedResult<RuntimeSettingDto>>;

