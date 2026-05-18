using GrammarCorrector.Models;
using Microsoft.EntityFrameworkCore;

namespace GrammarCorrector.Data;

public class ApplicationDbContext : DbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

    public DbSet<User> Users { get; set; } = null!;
    public DbSet<Subscription> Subscriptions { get; set; } = null!;
    public DbSet<UsageMetrics> UsageMetrics { get; set; } = null!;
    public DbSet<Payment> Payments { get; set; } = null!;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // User configuration
        modelBuilder.Entity<User>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.Email).IsUnique();
            entity.Property(e => e.Email).IsRequired().HasMaxLength(256);
            entity.Property(e => e.FullName).IsRequired().HasMaxLength(256);
            entity.Property(e => e.PasswordHash).IsRequired();
            entity.HasMany(e => e.UsageHistory)
                .WithOne(u => u.User)
                .HasForeignKey(u => u.UserId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasMany(e => e.Payments)
                .WithOne(p => p.User)
                .HasForeignKey(p => p.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // Subscription configuration
        modelBuilder.Entity<Subscription>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.Tier).IsUnique();
            entity.Property(e => e.Description).IsRequired().HasMaxLength(500);
            entity.Property(e => e.MonthlyPrice).HasPrecision(10, 2);
        });

        // UsageMetrics configuration
        modelBuilder.Entity<UsageMetrics>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => new { e.UserId, e.Year, e.Month }).IsUnique();
            entity.Property(e => e.Month).IsRequired();
            entity.Property(e => e.Year).IsRequired();
            entity.Property(e => e.CorrectionCount).IsRequired();
            entity.Property(e => e.TotalCharactersProcessed).IsRequired();
            entity.Property(e => e.TotalErrorsDetected).IsRequired();
        });

        // Payment configuration
        modelBuilder.Entity<Payment>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.StripePaymentIntentId).IsUnique();
            entity.Property(e => e.AmountInCents).HasPrecision(12, 0);
            entity.Property(e => e.StripePaymentIntentId).IsRequired().HasMaxLength(256);
        });

        // Seed subscription tiers
        SeedSubscriptionTiers(modelBuilder);
    }

    private static void SeedSubscriptionTiers(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Subscription>().HasData(
            new Subscription
            {
                Id = 1,
                Tier = SubscriptionTier.Free,
                MonthlyQuota = 500,
                MonthlyPrice = 0,
                Description = "Free tier with 500 corrections per month",
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            },
            new Subscription
            {
                Id = 2,
                Tier = SubscriptionTier.Unlimited,
                MonthlyQuota = null,
                MonthlyPrice = 9.99m,
                Description = "Unlimited corrections with priority support",
                StripePriceId = "price_unlimited_tier", // This should be set to actual Stripe price ID
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            }
        );
    }
}
