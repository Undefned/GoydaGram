using Microsoft.EntityFrameworkCore;
using UserService.Domain.Entities;

namespace UserService.Infrastructure.Persistence;

public class AppDbContext : DbContext
{
    public DbSet<User> Users { get; set; }
    public DbSet<Subscription> Subscriptions { get; set; }
    public DbSet<RefreshToken> RefreshTokens { get; set; }

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

        modelBuilder.Entity<RefreshToken>(entity =>
        {
            entity.HasKey(t => t.Id);
            entity.Property(t => t.TokenHash).HasMaxLength(64); // hex SHA-256 = 64 символа
            entity.HasIndex(t => t.TokenHash).IsUnique();
            entity.HasIndex(t => t.UserId); // для RevokeAllForUserAsync и уборки истёкших токенов
            entity.HasIndex(t => t.ExpiresAt); // для периодической чистки протухших записей

            entity.HasOne<User>()
                .WithMany()
                .HasForeignKey(t => t.UserId)
                .OnDelete(DeleteBehavior.Cascade); // удалили юзера — все его токены не нужны
        });
    }
}