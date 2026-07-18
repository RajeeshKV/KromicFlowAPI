using KromicFlow.Application.Common;
using KromicFlow.Application.DTOs.Admin;
using MediatR;

namespace KromicFlow.Application.Features.Admin.Notifications;

public sealed record SendNotificationCommand(Guid AdminId, Guid? UserId, string Channel, string Subject, string Body) : IRequest<Result<NotificationDto>>;
