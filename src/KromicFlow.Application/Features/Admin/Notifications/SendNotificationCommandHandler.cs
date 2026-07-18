using KromicFlow.Application.Abstractions;
using KromicFlow.Application.Common;
using KromicFlow.Application.DTOs.Admin;
using KromicFlow.Domain.Entities;
using KromicFlow.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace KromicFlow.Application.Features.Admin.Notifications;

internal sealed class SendNotificationCommandHandler(IKromicFlowDbContext db, INotificationSender notificationSender, IAuditWriter auditWriter) : IRequestHandler<SendNotificationCommand, Result<NotificationDto>>
{
    public async Task<Result<NotificationDto>> Handle(SendNotificationCommand request, CancellationToken cancellationToken)
    {
        var channel = Enum.TryParse<NotificationChannel>(request.Channel, true, out var parsed) ? parsed : NotificationChannel.Email;
        var message = new NotificationMessage { UserId = request.UserId, Audience = request.UserId.HasValue ? NotificationAudience.User : NotificationAudience.Broadcast, Channel = channel, Subject = request.Subject, Body = request.Body };
        db.NotificationMessages.Add(message);
        var users = request.UserId.HasValue ? await db.Users.Where(x => x.Id == request.UserId.Value).ToListAsync(cancellationToken) : await db.Users.Where(x => x.IsActive).ToListAsync(cancellationToken);
        foreach (var user in users)
        {
            if (await db.UserRestrictions.AnyAsync(x => x.UserId == user.Id && x.NotificationBlocked, cancellationToken)) continue;
            if (channel == NotificationChannel.Email && user.MarketingEmailEnabled) message.ProviderMessageId = await notificationSender.SendEmailAsync(user.Email, request.Subject, request.Body, cancellationToken);
            if (channel == NotificationChannel.Push && user.MarketingPushEnabled) message.ProviderMessageId = await notificationSender.SendPushAsync(user.Id, request.Subject, request.Body, cancellationToken);
        }
        message.Status = NotificationStatus.Sent;
        message.SentUtc = DateTime.UtcNow;
        await db.SaveChangesAsync(cancellationToken);
        await auditWriter.WriteAsync("NotificationSent", nameof(NotificationMessage), message.Id.ToString(), null, request.AdminId, null, cancellationToken);
        return Result<NotificationDto>.Success(new NotificationDto(message.Id, message.UserId, message.Audience.ToString(), message.Channel.ToString(), message.Status.ToString(), message.Subject, message.CreatedUtc, message.SentUtc));
    }
}
