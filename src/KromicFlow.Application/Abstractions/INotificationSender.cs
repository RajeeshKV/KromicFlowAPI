namespace KromicFlow.Application.Abstractions;

public interface INotificationSender
{
    Task<string?> SendEmailAsync(string toEmail, string subject, string body, CancellationToken cancellationToken);
    Task<string?> SendPushAsync(Guid userId, string subject, string body, CancellationToken cancellationToken);
}
