using KromicFlow.Application.Common;
using KromicFlow.Domain.Enums;
using MediatR;

namespace KromicFlow.Application.Features.Analytics.GetConversations;

public sealed record GetConversationsQuery(
    Guid UserId,
    Guid InstagramAccountId,
    int Page = 1,
    int PageSize = 25
) : IRequest<PagedResult<ConversationDto>>;

public sealed record ConversationDto(
    string CommenterIgId,
    string CommenterUsername,
    string LatestCommentText,
    string? MediaIgId,
    DateTime ReceivedUtc,
    WebhookStatus Status,
    bool PublicReplySent,
    bool PrivateReplySent,
    int TotalInteractions          // how many times this user has triggered an automation
);
