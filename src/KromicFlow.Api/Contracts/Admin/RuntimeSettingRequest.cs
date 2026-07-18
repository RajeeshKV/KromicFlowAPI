namespace KromicFlow.Api.Contracts.Admin;

public sealed record RuntimeSettingRequest(string Key, string Value, bool IsSecret, string? Description);
