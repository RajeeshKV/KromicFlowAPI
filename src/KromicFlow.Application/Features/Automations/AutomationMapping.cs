using System.Text.Json;
using KromicFlow.Application.DTOs.Automations;
using KromicFlow.Domain.Entities;
using KromicFlow.Domain.Enums;

namespace KromicFlow.Application.Features.Automations;

internal static class AutomationMapping
{
    public static AutomationDto ToDto(Automation automation) => new(
        automation.Id,
        automation.InstagramAccountId,
        automation.Name,
        automation.TriggerType.ToString(),
        JsonSerializer.Deserialize<string[]>(automation.KeywordsJson) ?? [],
        automation.PublicReply,
        automation.PrivateReply,
        automation.Enabled,
        automation.CooldownSeconds,
        automation.Priority);

    public static void Apply(Automation automation, string name, string triggerType, string[] keywords, string? publicReply, string? privateReply, int cooldownSeconds, int priority)
    {
        automation.Name = name.Trim();
        automation.TriggerType = Enum.TryParse<AutomationTriggerType>(triggerType, true, out var parsed) ? parsed : AutomationTriggerType.CommentKeyword;
        automation.KeywordsJson = JsonSerializer.Serialize(keywords.Select(x => x.Trim()).Where(x => x.Length > 0).Distinct(StringComparer.OrdinalIgnoreCase));
        automation.PublicReply = publicReply;
        automation.PrivateReply = privateReply;
        automation.CooldownSeconds = cooldownSeconds;
        automation.Priority = priority;
    }
}
