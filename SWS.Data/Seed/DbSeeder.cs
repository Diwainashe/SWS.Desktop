using System.Linq;
using SWS.Core.Models;

namespace SWS.Data.Seed
{
    /// <summary>
    /// Seeds a starter device + a small essential set of points for smoke testing.
    /// Safe to call on every startup: it only seeds when there are no devices yet.
    /// </summary>
    public static class DbSeeder
    {
        public static void SeedIfEmpty(SwsDbContext db)
        {
            // Only seed on a clean database
            if (db.DeviceConfigs.Any())
                return;

            var device = new DeviceConfig
            {
                Name = "GM9907-DEV",
                IpAddress = "192.168.1.5",
                Port = 502,
                UnitId = 1,
                PollMs = 1000,
                IsEnabled = true
            };

            db.DeviceConfigs.Add(device);
            db.SaveChanges();

            // IMPORTANT:
            // GM manuals often show holding registers like 40001, 40007, etc.
            // Open Modscan may display as 400001, 400007, etc (padded style).
            //
            // We keep YOUR convention: store "manual-style" 4xxxx addresses,
            // but you must be consistent in the conversion function when reading.

            // Point 1: Your current test register (likely error/status style)
            db.PointConfigs.Add(new PointConfig
            {
                DeviceConfigId = device.Id,
                Key = "Test.StatusOrError",
                Address = 40007,              // prefer 40007 (manual style), not 400007
                Length = 1,
                DataType = PointDataType.Int16,
                Scale = 1m,
                PollRateMs = 500,
                IsEssential = true
            });

            // Point 2: Display Weight as Float32 (often 2 registers)
            // Many devices provide weight as float across 2 holding registers.
            db.PointConfigs.Add(new PointConfig
            {
                DeviceConfigId = device.Id,
                Key = "Weight.Display",
                Address = 40007,              // if your manual truly says weight is 40007-40008
                Length = 2,
                DataType = PointDataType.Float32,
                Scale = 1m,
                PollRateMs = 250,
                IsEssential = true
            });

            db.SaveChanges();
        }
    }
}
