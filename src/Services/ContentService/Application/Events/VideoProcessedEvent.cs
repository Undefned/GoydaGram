namespace ContentService.Application.Events;

public record VideoProcessedEvent(Guid VideoId, Guid UserId, string HlsPlaylistUrl);