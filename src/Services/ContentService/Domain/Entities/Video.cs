using ContentService.Domain.Enums;

namespace ContentService.Domain.Entities;

public class Video
{
    public Guid Id { get; private set; }
    public Guid UserId { get; private set; }
    public string Title { get; private set; } = string.Empty;
    public string Description { get; private set; } = string.Empty;
    public int Duration { get; private set; } // seconds

    // Сырой ключ объекта в MinIO (например videos/{userId}/{id}.mp4) — по нему
    // фоновая транскодизация скачивает исходник. Отдельно от Url, потому что
    // Url — это публичный proxy-путь через StreamController, а не путь в бакете.
    public string StorageKey { get; private set; } = string.Empty;

    public string Url { get; private set; } = string.Empty;
    public string PreviewUrl { get; private set; } = string.Empty;

    // Null, пока HLS не сгенерирован фоновым воркером. Прогрессивный MP4 (Url)
    // доступен сразу — HLS доезжает отдельно и не блокирует появление видео в ленте.
    public string? HlsPlaylistUrl { get; private set; }

    public VideoStatus Status { get; private set; } = VideoStatus.Processing;
    public int ViewsCount { get; private set; }
    public int LikesCount { get; private set; }
    public int CommentsCount { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime UpdatedAt { get; private set; }

    private readonly List<Tag> _tags = new();
    public IReadOnlyCollection<Tag> Tags => _tags.AsReadOnly();

    private Video() { }

    public static Video Create(Guid userId, string title, string description, int duration, string storageKey, string url)
    {
        return new Video
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            Title = title,
            Description = description,
            Duration = duration,
            StorageKey = storageKey,
            Url = url,
            Status = VideoStatus.Processing,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
    }

    public void MarkAsReady(string previewUrl)
    {
        PreviewUrl = previewUrl;
        Status = VideoStatus.Ready;
        UpdatedAt = DateTime.UtcNow;
    }

    public void MarkAsFailed()
    {
        Status = VideoStatus.Failed;
        UpdatedAt = DateTime.UtcNow;
    }

    public void SetHlsPlaylist(string playlistUrl)
    {
        HlsPlaylistUrl = playlistUrl;
        UpdatedAt = DateTime.UtcNow;
    }

    public void AddTag(Tag tag)
    {
        if (!_tags.Any(t => t.Id == tag.Id))
            _tags.Add(tag);
    }

    public void AddTags(IEnumerable<Tag> tags)
    {
        foreach (var tag in tags)
            AddTag(tag);
    }

    public void IncrementViews() => ViewsCount++;
    public void IncrementLikes() => LikesCount++;
    public void IncrementComments() => CommentsCount++;
}