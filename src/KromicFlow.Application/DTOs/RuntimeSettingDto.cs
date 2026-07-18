namespace KromicFlow.Application.DTOs.Admin;

public sealed record RuntimeSettingDto(string Key, string Value, bool IsSecret, string? Description);
