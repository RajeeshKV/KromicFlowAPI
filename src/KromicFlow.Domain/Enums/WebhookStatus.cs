namespace KromicFlow.Domain.Enums;

public enum WebhookStatus
{
    Pending = 0,
    Processing = 1,
    Completed = 2,
    Failed = 3,
    DeadLetter = 4
}
