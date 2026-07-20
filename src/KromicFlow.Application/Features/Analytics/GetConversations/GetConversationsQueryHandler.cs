using KromicFlow.Application.Abstractions;
using KromicFlow.Application.Common;
using KromicFlow.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace KromicFlow.Application.Features.Analytics.GetConversations;

internal sealed class GetConversationsQueryHandler(IKromicFlowDbContext db)
    : IRequestHandler<GetConversationsQuery, PagedResult<ConversationDto>>
{
    public async Task<PagedResult<ConversationDto>> Handle(GetConversationsQuery request, CancellationToken cancellationToken)
    {
        var account = await db.InstagramAccounts
            .FirstOrDefaultAsync(x => x.Id == request.InstagramAccountId && x.UserId == request.UserId, cancellationToken);

        if (account is null)
            return new PagedResult<ConversationDto>([], request.Page, request.PageSize, 0);

        // Only events that have a commenter (i.e. parsed comment payloads)
        var baseQuery = db.WebhookEvents
            .Where(x => x.InstagramAccountId == request.InstagramAccountId
                     && x.CommenterIgId != null
                     && x.Status != WebhookStatus.Skipped);

        // Count distinct commenters for pagination
        var totalDistinct = await baseQuery
            .Select(x => x.CommenterIgId)
            .Distinct()
            .CountAsync(cancellationToken);

        // Get the latest event per commenter, ordered by most recent first
        // Use a subquery: for each commenter get max ReceivedUtc then join back
        var latestPerCommenter = await baseQuery
            .GroupBy(x => x.CommenterIgId!)
            .Select(g => new
            {
                CommenterIgId = g.Key,
                LatestId = g.OrderByDescending(x => x.ReceivedUtc).First().Id,
                TotalInteractions = g.Count()
            })
            .OrderByDescending(x => x.LatestId)  // proxy for most-recent sort
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .ToListAsync(cancellationToken);

        if (latestPerCommenter.Count == 0)
            return new PagedResult<ConversationDto>([], request.Page, request.PageSize, totalDistinct);

        var latestEventIds = latestPerCommenter.Select(x => x.LatestId).ToList();
        var interactionMap = latestPerCommenter.ToDictionary(x => x.CommenterIgId, x => x.TotalInteractions);

        var events = await db.WebhookEvents
            .Where(x => latestEventIds.Contains(x.Id))
            .ToListAsync(cancellationToken);

        var conversations = events
            .OrderByDescending(x => x.ReceivedUtc)
            .Select(e => new ConversationDto(
                CommenterIgId: e.CommenterIgId!,
                CommenterUsername: e.CommenterUsername ?? e.CommenterIgId!,
                LatestCommentText: e.CommentText ?? string.Empty,
                MediaIgId: e.MediaIgId,
                ReceivedUtc: e.ReceivedUtc,
                Status: e.Status,
                PublicReplySent: e.PublicReplySentUtc.HasValue,
                PrivateReplySent: e.PrivateReplySentUtc.HasValue,
                TotalInteractions: interactionMap.GetValueOrDefault(e.CommenterIgId!, 1)
            ))
            .ToList();

        return new PagedResult<ConversationDto>(conversations, request.Page, request.PageSize, totalDistinct);
    }
}
