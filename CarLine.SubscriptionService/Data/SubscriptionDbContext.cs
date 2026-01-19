using CarLine.SubscriptionService.Data.Entities;
using Microsoft.EntityFrameworkCore;

namespace CarLine.SubscriptionService.Data;

public sealed class SubscriptionDbContext(DbContextOptions<SubscriptionDbContext> options) : DbContext(options)
{
    public DbSet<SubscriptionEntity> Subscriptions => Set<SubscriptionEntity>();
    public DbSet<SubscriptionNotificationEntity> SubscriptionNotifications => Set<SubscriptionNotificationEntity>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<SubscriptionEntity>(b =>
        {
            b.ToTable("Subscriptions");
            b.HasKey(x => x.Id);
            b.Property(x => x.Email).HasMaxLength(320).IsRequired();

            b.Property(x => x.Manufacturer).HasMaxLength(100);
            b.Property(x => x.Model).HasMaxLength(100);
            b.Property(x => x.Condition).HasMaxLength(30);
            b.Property(x => x.Fuel).HasMaxLength(30);
            b.Property(x => x.Transmission).HasMaxLength(30);
            b.Property(x => x.Type).HasMaxLength(30);
            b.Property(x => x.Region).HasMaxLength(80);

            b.Property(x => x.CreatedAtUtc).IsRequired();
            b.Property(x => x.SinceUtc).IsRequired();
            b.Property(x => x.IsActive).IsRequired();

            b.HasIndex(x => x.Email);
            b.HasIndex(x => new { x.IsActive, x.SinceUtc });
        });

        modelBuilder.Entity<SubscriptionNotificationEntity>(b =>
        {
            b.ToTable("SubscriptionNotifications");
            b.HasKey(x => x.Id);

            b.Property(x => x.CarId).HasMaxLength(256).IsRequired();
            b.Property(x => x.CarUrl).HasMaxLength(2048);
            b.Property(x => x.DetectedAtUtc).IsRequired();

            b.HasOne(x => x.Subscription)
                .WithMany(s => s.Notifications)
                .HasForeignKey(x => x.SubscriptionId)
                .OnDelete(DeleteBehavior.Cascade);

            b.HasIndex(x => new { x.SubscriptionId, x.CarId }).IsUnique();
            b.HasIndex(x => new { x.SubscriptionId, x.DetectedAtUtc });
        });
    }
}