using KromicFlow.Application.Common;
using KromicFlow.Application.DTOs;
using KromicFlow.Domain.Enums;
using MediatR;

namespace KromicFlow.Application.Features.Instagram.GetInstagramMedia;

public sealed record GetInstagramMediaQuery(
    Guid InstagramAccountId,
    int Page = 1,
    int PageSize = 20,
    MediaType? MediaType = null,
    string? Search = null
) : IRequest<Result<MediaPaginationResponseDto>>;
