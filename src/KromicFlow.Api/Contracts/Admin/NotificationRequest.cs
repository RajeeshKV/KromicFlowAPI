namespace KromicFlow.Api.Contracts.Admin;

public sealed record NotificationRequest(string Channel, string Subject, string Body);
