using Microsoft.EntityFrameworkCore;
using SWS.Core.Models;

namespace SWS.Data
{
    public sealed class SwsDbContext : DbContext
    {
        public SwsDbContext(DbContextOptions<SwsDbContext> options) : base(options) { }

        public DbSet<DeviceConfig> DeviceConfigs => Set<DeviceConfig>();
        public DbSet<PointConfig> PointConfigs => Set<PointConfig>();
        public DbSet<LatestReading> LatestReadings => Set<LatestReading>();
        public DbSet<ReadingHistory> ReadingHistories => Set<ReadingHistory>();
        public DbSet<PointTemplate> PointTemplates { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // LatestReading: enforce "one row per device+point"
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

            modelBuilder.Entity<PointTemplate>()
                .Property(p => p.Scale)
                .HasPrecision(18, 6);  // Adjust precision and scale as needed
        }
    }
}