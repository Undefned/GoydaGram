namespace ContentService.Application.Events;

public record VideoUploadedEvent(Guid VideoId, Guid UserId, string Title, List<string> Tags, DateTime UploadedAt);
