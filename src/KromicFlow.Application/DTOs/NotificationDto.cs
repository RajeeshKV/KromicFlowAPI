namespace KromicFlow.Application.DTOs.Admin;

public sealed record NotificationDto(Guid Id, Guid? UserId, string Audience, string Channel, string Status, string Subject, DateTime CreatedUtc, DateTime? SentUtc);
