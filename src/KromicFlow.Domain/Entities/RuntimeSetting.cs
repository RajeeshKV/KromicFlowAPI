namespace KromicFlow.Domain.Entities;

public sealed class RuntimeSetting : Entity
{
    public string Key { get; set; } = string.Empty;
    public string Value { get; set; } = string.Empty;
    public bool IsSecret { get; set; }
    public string? Description { get; set; }
}
