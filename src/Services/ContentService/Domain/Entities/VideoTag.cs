namespace ContentService.Domain.Entities;
public class VideoTag
{
    public Guid VideoId { get; set; }
    public Video Video { get; set; } = null!;
    public Guid TagId { get; set; }
    public Tag Tag { get; set; } = null!;
}