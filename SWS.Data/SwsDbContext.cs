using Microsoft.EntityFrameworkCore;
using SWS.Core.Models;

namespace SWS.Data;

public sealed class SwsDbContext : DbContext
{
    public SwsDbContext(DbContextOptions<SwsDbContext> options) : base(options) { }

    public DbSet<DeviceConfig> DeviceConfigs => Set<DeviceConfig>();
    public DbSet<PointConfig> PointConfigs => Set<PointConfig>();
    public DbSet<LatestReading> LatestReadings => Set<LatestReading>();
    public DbSet<ReadingHistory> ReadingHistories => Set<ReadingHistory>();

    // IMPORTANT: use expression-bodied DbSet so EF always picks it up cleanly
    public DbSet<PointTemplate> PointTemplates => Set<PointTemplate>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<LatestReading>(b =>
        {
            b.HasIndex(x => new { x.DeviceConfigId, x.PointConfigId }).IsUnique();
            b.Property(x => x.ValueNumeric).HasPrecision(18, 6);
        });

        modelBuilder.Entity<PointConfig>(b =>
        {
            b.Property(x => x.Scale).HasPrecision(18, 6);
        });

        modelBuilder.Entity<ReadingHistory>(b =>
        {
            b.HasKey(x => x.Id);
            b.HasIndex(x => new { x.PointConfigId, x.TimestampUtc });
            b.HasIndex(x => new { x.DeviceConfigId, x.TimestampUtc });
            b.Property(x => x.ValueNumeric).HasPrecision(18, 6);
        });

        modelBuilder.Entity<PointTemplate>(b =>
        {
            // Keep Key unique so templates don’t duplicate
            b.HasIndex(x => x.Key).IsUnique();

            // SQL Server does NOT have ushort. Store it as int in DB.
            b.Property(x => x.DefaultLength)
             .HasConversion<int>();

            // Decimal precision for scale
            b.Property(x => x.Scale)
             .HasPrecision(18, 6);

            // Optional: make these required-ish (prevents null surprises)
            b.Property(x => x.Key).HasMaxLength(128);
            b.Property(x => x.Label).HasMaxLength(128);
            b.Property(x => x.Unit).HasMaxLength(32);
            b.Property(x => x.DeviceType).HasMaxLength(64);
        });
    }
}