using Microsoft.EntityFrameworkCore;
using UserService.Domain.Entities;

namespace UserService.Infrastructure.Persistence;

public class AppDbContext : DbContext
{
    public DbSet<User> Users { get; set; }
    public DbSet<Subscription> Subscriptions { get; set; }

    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<User>(entity =>
        {
            entity.HasKey(u => u.Id);
            entity.HasIndex(u => u.Email).IsUnique();
            entity.HasIndex(u => u.Username).IsUnique();
            entity.HasIndex(u => u.DeletedAt).HasFilter("deleted_at IS NULL");
            entity.Property(u => u.Username).HasMaxLength(50);
            entity.Property(u => u.Email).HasMaxLength(100);
            entity.Property(u => u.Bio).HasMaxLength(500);
        });

        // Subscription — самостоятельная сущность, связь с User только через FK,
        // без навигационных свойств в обе стороны (у User их больше нет).
        modelBuilder.Entity<Subscription>(entity =>
        {
            entity.HasKey(s => s.Id);
            entity.HasIndex(s => new { s.FollowerId, s.FolloweeId }).IsUnique();
            entity.HasIndex(s => s.FollowerId);
            entity.HasIndex(s => s.FolloweeId);

            entity.HasOne<User>()
                .WithMany()
                .HasForeignKey(s => s.FollowerId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne<User>()
                .WithMany()
                .HasForeignKey(s => s.FolloweeId)
                .OnDelete(DeleteBehavior.Restrict);
        });
    }
}