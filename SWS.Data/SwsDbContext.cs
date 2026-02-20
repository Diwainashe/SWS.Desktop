using Microsoft.EntityFrameworkCore;
using SWS.Core.Models;

namespace SWS.Data;

/// <summary>
/// EF Core DbContext for configuration and live/latest readings.
/// Historian (Readings) will be added after the first vertical slice.
/// </summary>
public sealed class SwsDbContext : DbContext
{
    public SwsDbContext(DbContextOptions<SwsDbContext> options) : base(options) { }

    public DbSet<DeviceConfig> DeviceConfigs => Set<DeviceConfig>();
    public DbSet<PointConfig> PointConfigs => Set<PointConfig>();
    public DbSet<LatestReading> LatestReadings => Set<LatestReading>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<LatestReading>()
            .Property(x => x.ValueNumeric)
            .HasPrecision(18, 6);

        modelBuilder.Entity<PointConfig>()
            .Property(x => x.Scale)
            .HasPrecision(18, 6);
    }


}
