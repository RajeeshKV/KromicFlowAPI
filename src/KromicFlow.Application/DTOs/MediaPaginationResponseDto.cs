namespace KromicFlow.Application.DTOs;

public sealed record MediaPaginationResponseDto
{
    public List<InstagramMediaDto> Items { get; init; } = [];
    public int Total { get; init; }
}
