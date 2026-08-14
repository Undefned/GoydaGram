using Microsoft.EntityFrameworkCore;
using ContentService.Domain.Entities;

namespace ContentService.Infrastructure.Persistence;

public class AppDbContext : DbContext
{
    public DbSet<Video> Videos { get; set; }
    public DbSet<Tag> Tags { get; set; }
    public DbSet<VideoTag> VideoTags { get; set; }

    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Video>(entity =>
        {
            entity.HasKey(v => v.Id);
            entity.HasIndex(v => v.UserId);
            entity.HasIndex(v => v.Status);
            entity.HasIndex(v => v.CreatedAt);
            entity.Property(v => v.Title).HasMaxLength(255);
            entity.Property(v => v.Description).HasMaxLength(2000);
            
            entity.HasMany(v => v.Tags)
                .WithMany()
                .UsingEntity<VideoTag>(
                    j => j.HasOne(vt => vt.Tag).WithMany().HasForeignKey(vt => vt.TagId),
                    j => j.HasOne(vt => vt.Video).WithMany().HasForeignKey(vt => vt.VideoId)
                );
        });

        modelBuilder.Entity<Tag>(entity =>
        {
            entity.HasKey(t => t.Id);
            entity.HasIndex(t => t.Name).IsUnique();
            entity.Property(t => t.Name).HasMaxLength(50);
        });

        modelBuilder.Entity<VideoTag>(entity =>
        {
            entity.HasKey(vt => new { vt.VideoId, vt.TagId });
        });
    }
}