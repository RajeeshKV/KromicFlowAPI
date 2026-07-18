namespace KromicFlow.Application.DTOs.Automations;

public sealed record AutomationDto(Guid Id, Guid InstagramAccountId, string Name, string TriggerType, string[] Keywords, string? PublicReply, string? PrivateReply, bool Enabled, int CooldownSeconds, int Priority);
