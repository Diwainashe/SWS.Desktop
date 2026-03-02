using Microsoft.EntityFrameworkCore;
using SWS.Core.Models;
using SWS.Data;
using System.ComponentModel.DataAnnotations;

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

    public async Task<int> AddDefaultPointsForDeviceAsync(
    int deviceId,
    IReadOnlyList<SWS.Core.Models.PointConfig> points,
    CancellationToken ct)
    {
        await using var db = await _dbFactory.CreateDbContextAsync(ct);

        // Avoid duplicates by Key (per device)
        var existingKeys = await db.PointConfigs
            .Where(p => p.DeviceConfigId == deviceId)
            .Select(p => p.Key)
            .ToListAsync(ct);

        int added = 0;

        foreach (var p in points)
        {
            if (existingKeys.Contains(p.Key))
                continue;

            db.PointConfigs.Add(p);
            added++;
        }

        await db.SaveChangesAsync(ct);
        return added;
    }

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

    public async Task<List<LatestReadingSnapshot>> GetLatestReadingsAsync(CancellationToken ct)
    {
        await using var db = await _dbFactory.CreateDbContextAsync(ct);

        return await (
            from r in db.LatestReadings.AsNoTracking()
            join d in db.DeviceConfigs.AsNoTracking() on r.DeviceConfigId equals d.Id
            join p in db.PointConfigs.AsNoTracking() on r.PointConfigId equals p.Id
            orderby d.Name, p.Key
            select new LatestReadingSnapshot
            {
                DeviceId = d.Id,
                DeviceName = d.Name,
                DeviceType = d.DeviceType,

                PointId = p.Id,
                Key = p.Key,
                Label = p.Label,
                Unit = p.Unit,
                DataType = p.DataType,                // ✅ THIS fixes the ON/OFF confusion

                ValueNumeric = r.ValueNumeric,
                Quality = r.Quality,
                TimestampLocal = r.TimestampLocal
            }
        ).ToListAsync(ct);
    }

    // ---------------- Templates ----------------

    public async Task<List<PointTemplate>> GetTemplatesForDeviceTypeAsync(DeviceType type, CancellationToken ct)
    {
        await using var db = await _dbFactory.CreateDbContextAsync(ct);

        string typeName = type.ToString();

        return await db.PointTemplates.AsNoTracking()
            .Where(t => t.DeviceType == typeName)
            .OrderBy(t => t.Key)
            .ToListAsync(ct);
    }

    /// <summary>
    /// Copies templates -> PointConfigs for a device, skipping duplicate Keys.
    /// Returns number of points added.
    /// </summary>
    public async Task<int> AddDefaultPointsFromTemplatesAsync(
     int deviceId,
     DeviceType deviceType,
     CancellationToken ct)
    {
        await using var db = await _dbFactory.CreateDbContextAsync(ct);

        string typeKey = deviceType.ToString(); // e.g. "GM9907_L5"

        // Templates for that device type
        var templates = await db.PointTemplates
            .AsNoTracking()
            .Where(t => t.DeviceType == typeKey)
            .OrderBy(t => t.Key)
            .ToListAsync(ct);

        if (templates.Count == 0)
            return 0;

        // Avoid duplicates (per device) by Key
        var existingKeys = await db.PointConfigs
            .Where(p => p.DeviceConfigId == deviceId)
            .Select(p => p.Key)
            .ToListAsync(ct);

        int added = 0;

        foreach (var t in templates)
        {
            if (existingKeys.Contains(t.Key))
                continue;

            db.PointConfigs.Add(new PointConfig
            {
                DeviceConfigId = deviceId,
                Key = t.Key,
                Label = t.Label,
                Unit = t.Unit,
                Area = t.Area,
                Address = t.Address,
                Length = t.DefaultLength,
                DataType = t.DataType,
                Scale = t.Scale,
                PollRateMs = t.PollRateMs,
                IsEssential = t.IsEssential,
                LogToHistory = t.LogToHistory,
                HistoryIntervalMs = t.HistoryIntervalMs
            });

            added++;
        }

        await db.SaveChangesAsync(ct);
        return added;
    }
}

