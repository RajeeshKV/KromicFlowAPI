namespace KromicFlow.Application.Abstractions;

public interface INotificationSender
{
    Task<string?> SendEmailAsync(string toEmail, string subject, string body, CancellationToken cancellationToken);
    
    /// <summary>
    /// Send email using Brevo transactional template
    /// </summary>
    Task<string?> SendEmailWithTemplateAsync(string toEmail, int templateId, Dictionary<string, string> templateParams, CancellationToken cancellationToken);
    
    Task<string?> SendPushAsync(Guid userId, string subject, string body, CancellationToken cancellationToken);
}
