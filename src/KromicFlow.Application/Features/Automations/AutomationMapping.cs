using System.Text.Json;
using KromicFlow.Application.Abstractions;
using KromicFlow.Application.DTOs.Automations;
using KromicFlow.Domain.Entities;
using KromicFlow.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace KromicFlow.Application.Features.Automations;

internal static class AutomationMapping
{
    public static async Task<AutomationDto> ToDtoAsync(Automation automation, IKromicFlowDbContext db, CancellationToken cancellationToken)
    {
        var selectedMedia = await db.AutomationMedia
            .Include(x => x.InstagramMedia)
            .Where(x => x.AutomationId == automation.Id)
            .Select(x => new MediaForAutomationDto(
                x.InstagramMedia.Id,
                x.InstagramMedia.InstagramMediaId,
                x.InstagramMedia.Caption,
                x.InstagramMedia.ThumbnailUrl,
                x.InstagramMedia.PostedAtUtc
            ))
            .ToListAsync(cancellationToken);

        return new AutomationDto(
            automation.Id,
            automation.InstagramAccountId,
            automation.Name,
            automation.Scope,
            automation.TriggerType,
            JsonSerializer.Deserialize<string[]>(automation.KeywordsJson) ?? [],
            automation.PublicReply,
            automation.PrivateReply,
            automation.Enabled,
            automation.CooldownSeconds,
            automation.Priority,
            selectedMedia);
    }

    public static void Apply(Automation automation, string name, AutomationScope scope, AutomationTriggerType triggerType, string[] keywords, string? publicReply, string? privateReply, int cooldownSeconds, int priority)
    {
        automation.Name = name.Trim();
        automation.Scope = scope;
        automation.TriggerType = triggerType;
        automation.KeywordsJson = JsonSerializer.Serialize(keywords.Select(x => x.Trim()).Where(x => x.Length > 0).Distinct(StringComparer.OrdinalIgnoreCase));
        automation.PublicReply = publicReply;
        automation.PrivateReply = privateReply;
        automation.CooldownSeconds = cooldownSeconds;
        automation.Priority = priority;
    }
}
