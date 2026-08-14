namespace ContentService.Domain.Entities;

public class Tag
{
    public Guid Id { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public DateTime CreatedAt { get; private set; }

    private Tag() { }

    public static Tag Create(string name)
    {
        return new Tag
        {
            Id = Guid.NewGuid(),
            Name = name.ToLowerInvariant(),
            CreatedAt = DateTime.UtcNow
        };
    }
}