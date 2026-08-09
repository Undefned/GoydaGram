using ContentService.Domain.Enums;

namespace ContentService.Domain.Entities;

public class Video
{
    public Guid Id { get; private set; }
    public Guid UserId { get; private set; }
    public string Title { get; private set; } = string.Empty;
    public string Description { get; private set; } = string.Empty;
    public int Duration { get; private set; } // seconds
    public string Url { get; private set; } = string.Empty;
    public string PreviewUrl { get; private set; } = string.Empty;
    public VideoStatus Status { get; private set; } = VideoStatus.Processing;
    public int ViewsCount { get; private set; }
    public int LikesCount { get; private set; }
    public int CommentsCount { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime UpdatedAt { get; private set; }

    private readonly List<Tag> _tags = new();
    public IReadOnlyCollection<Tag> Tags => _tags.AsReadOnly();

    private Video() { }

    public static Video Create(Guid userId, string title, string description, int duration, string url)
    {
        return new Video
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            Title = title,
            Description = description,
            Duration = duration,
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