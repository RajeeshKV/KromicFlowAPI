using KromicFlow.Application.Common;
using KromicFlow.Application.DTOs.Admin;
using MediatR;

namespace KromicFlow.Application.Features.Admin.Notifications;

public sealed record ListNotificationsQuery(int Page = 1, int PageSize = 25) : IRequest<PagedResult<NotificationDto>>;

