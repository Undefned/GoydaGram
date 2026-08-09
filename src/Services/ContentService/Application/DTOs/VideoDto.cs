namespace ContentService.Application.DTOs;

public record VideoDto(
    Guid Id,
    Guid UserId,
    string Title,
    string Description,
    int Duration,
    string Url,
    string PreviewUrl,
    string Status,
    int ViewsCount,
    int LikesCount,
    int CommentsCount,
    DateTime CreatedAt,
    List<string> Tags
);