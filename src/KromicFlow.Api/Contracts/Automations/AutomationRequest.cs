namespace KromicFlow.Api.Contracts.Automations;

public sealed record AutomationRequest(Guid InstagramAccountId, string Name, string TriggerType, string[] Keywords, string? PublicReply, string? PrivateReply, int CooldownSeconds, int Priority);
