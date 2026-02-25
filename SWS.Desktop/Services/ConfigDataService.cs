using Microsoft.EntityFrameworkCore;
using SWS.Core.Models;
using SWS.Data;

namespace SWS.Desktop.Services;

public sealed class ConfigDataService
{
    private readonly IDbContextFactory<SwsDbContext> _dbFactory;

    public ConfigDataService(IDbContextFactory<SwsDbContext> dbFactory)
    {
        _dbFactory = dbFactory;
    }

    // ---------------- Devices ----------------

    public async Task<List<DeviceConfig>> GetDevicesAsync(CancellationToken ct)
    {
        await using var db = await _dbFactory.CreateDbContextAsync(ct);
        return await db.DeviceConfigs.AsNoTracking().OrderBy(d => d.Name).ToListAsync(ct);
    }

    public async Task AddDeviceAsync(DeviceConfig device, CancellationToken ct)
    {
        await using var db = await _dbFactory.CreateDbContextAsync(ct);
        db.DeviceConfigs.Add(device);
        await db.SaveChangesAsync(ct);
    }

    public async Task UpdateDeviceAsync(DeviceConfig device, CancellationToken ct)
    {
        await using var db = await _dbFactory.CreateDbContextAsync(ct);
        db.DeviceConfigs.Update(device);
        await db.SaveChangesAsync(ct);
    }

    public async Task DeleteDeviceAsync(int deviceId, CancellationToken ct)
    {
        await using var db = await _dbFactory.CreateDbContextAsync(ct);

        var dev = await db.DeviceConfigs.FirstOrDefaultAsync(d => d.Id == deviceId, ct);
        if (dev == null) return;

        // remove points & latest/history too (MVP)
        var points = db.PointConfigs.Where(p => p.DeviceConfigId == deviceId);
        db.PointConfigs.RemoveRange(points);

        db.DeviceConfigs.Remove(dev);
        await db.SaveChangesAsync(ct);
    }

    // ---------------- Points ----------------

    public async Task<List<PointConfig>> GetPointsForDeviceAsync(int deviceId, CancellationToken ct)
    {
        await using var db = await _dbFactory.CreateDbContextAsync(ct);
        return await db.PointConfigs.AsNoTracking()
            .Where(p => p.DeviceConfigId == deviceId)
            .OrderBy(p => p.Key)
            .ToListAsync(ct);
    }

    public async Task AddPointAsync(PointConfig point, CancellationToken ct)
    {
        await using var db = await _dbFactory.CreateDbContextAsync(ct);
        db.PointConfigs.Add(point);
        await db.SaveChangesAsync(ct);
    }

    public async Task UpdatePointAsync(PointConfig point, CancellationToken ct)
    {
        await using var db = await _dbFactory.CreateDbContextAsync(ct);
        db.PointConfigs.Update(point);
        await db.SaveChangesAsync(ct);
    }

    public async Task DeletePointAsync(int pointId, CancellationToken ct)
    {
        await using var db = await _dbFactory.CreateDbContextAsync(ct);

        var p = await db.PointConfigs.FirstOrDefaultAsync(x => x.Id == pointId, ct);
        if (p == null) return;

        db.PointConfigs.Remove(p);
        await db.SaveChangesAsync(ct);
    }

    // ---------------- Live readings (dashboard) ----------------

    public async Task<List<LatestReadingRow>> GetLatestReadingsAsync(CancellationToken ct)
    {
        await using var db = await _dbFactory.CreateDbContextAsync(ct);

        return await (
            from r in db.LatestReadings.AsNoTracking()
            join d in db.DeviceConfigs.AsNoTracking() on r.DeviceConfigId equals d.Id
            join p in db.PointConfigs.AsNoTracking() on r.PointConfigId equals p.Id
            orderby d.Name, p.Key
            select new LatestReadingRow
            {
                DeviceId = d.Id,
                DeviceName = d.Name,
                PointId = p.Id,
                Key = p.Key,
                Label = p.Label,
                Unit = p.Unit,
                ValueNumeric = r.ValueNumeric,
                Quality = r.Quality,
                TimestampLocal = r.TimestampLocal
            }
        ).ToListAsync(ct);
    }
}

public sealed class LatestReadingRow
{
    public int DeviceId { get; set; }
    public string DeviceName { get; set; } = "";
    public int PointId { get; set; }
    public string Key { get; set; } = "";
    public string Label { get; set; } = "";
    public string Unit { get; set; } = "";
    public decimal? ValueNumeric { get; set; }
    public SWS.Core.Models.ReadingQuality Quality { get; set; }
    public DateTime TimestampLocal { get; set; }
}