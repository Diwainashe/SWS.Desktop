using Microsoft.EntityFrameworkCore;
using SWS.Core.Models;
using SWS.Data;

namespace SWS.Desktop.Services;

/// <summary>
/// Provides history data for trending.
/// Scoped — one instance per navigation scope.
/// </summary>
public sealed class TrendDataService
{
    private readonly IDbContextFactory<SwsDbContext> _dbFactory;

    public TrendDataService(IDbContextFactory<SwsDbContext> dbFactory)
    {
        _dbFactory = dbFactory;
    }

    /// <summary>
    /// Returns all points that have at least one history record,
    /// grouped by device. Used to populate the point picker.
    /// </summary>
    public async Task<List<TrendablePointDto>> GetTrendablePointsAsync(CancellationToken ct)
    {
        await using var db = await _dbFactory.CreateDbContextAsync(ct);

        // Join PointConfigs + DeviceConfigs, filter to points that actually have history
        var pointsWithHistory = await db.ReadingHistories
            .AsNoTracking()
            .Select(h => h.PointConfigId)
            .Distinct()
            .ToListAsync(ct);

        if (pointsWithHistory.Count == 0)
            return new List<TrendablePointDto>();

        var hashSet = pointsWithHistory.ToHashSet();

        return await (
            from p in db.PointConfigs.AsNoTracking()
            join d in db.DeviceConfigs.AsNoTracking() on p.DeviceConfigId equals d.Id
            where hashSet.Contains(p.Id)
            orderby d.Name, p.Label
            select new TrendablePointDto
            {
                PointConfigId = p.Id,
                DeviceConfigId = d.Id,
                DeviceName = d.Name,
                Key = p.Key,
                Label = p.Label,
                Unit = p.Unit
            }
        ).ToListAsync(ct);
    }

    /// <summary>
    /// Returns history rows for one point within a time window,
    /// downsampled if the result set would exceed <paramref name="maxPoints"/>.
    /// </summary>
    public async Task<List<TrendPointDto>> GetHistoryAsync(
        int pointConfigId,
        DateTime from,
        DateTime to,
        int maxPoints,
        CancellationToken ct)
    {
        await using var db = await _dbFactory.CreateDbContextAsync(ct);

        var rows = await db.ReadingHistories
            .AsNoTracking()
            .Where(h => h.PointConfigId == pointConfigId
                     && h.TimestampLocal >= from
                     && h.TimestampLocal <= to
                     && h.Quality == ReadingQuality.Good
                     && h.ValueNumeric != null)
            .OrderBy(h => h.TimestampLocal)
            .Select(h => new TrendPointDto
            {
                Timestamp = h.TimestampLocal,
                Value = h.ValueNumeric!.Value
            })
            .ToListAsync(ct);

        // Downsample using Largest Triangle Three Buckets (LTTB) if too many points
        return rows.Count > maxPoints
            ? Lttb(rows, maxPoints)
            : rows;
    }

    // ── Largest Triangle Three Buckets downsampling ─────────────────────
    // Preserves shape fidelity better than simple skip-N decimation.

    private static List<TrendPointDto> Lttb(List<TrendPointDto> data, int threshold)
    {
        int length = data.Count;
        if (threshold >= length || threshold < 3)
            return data;

        var sampled = new List<TrendPointDto>(threshold) { data[0] };
        double bucketSize = (double)(length - 2) / (threshold - 2);

        int a = 0;
        for (int i = 0; i < threshold - 2; i++)
        {
            int avgStart = (int)Math.Floor((i + 1) * bucketSize) + 1;
            int avgEnd = (int)Math.Floor((i + 2) * bucketSize) + 1;
            avgEnd = Math.Min(avgEnd, length);

            double avgX = 0, avgY = 0;
            int avgCount = avgEnd - avgStart;
            for (int j = avgStart; j < avgEnd; j++)
            {
                avgX += data[j].Timestamp.Ticks;
                avgY += (double)data[j].Value;
            }
            avgX /= avgCount;
            avgY /= avgCount;

            int rangeStart = (int)Math.Floor(i * bucketSize) + 1;
            int rangeEnd = (int)Math.Floor((i + 1) * bucketSize) + 1;
            rangeEnd = Math.Min(rangeEnd, length);

            double maxArea = -1;
            int maxIdx = rangeStart;
            double ax = data[a].Timestamp.Ticks, ay = (double)data[a].Value;

            for (int j = rangeStart; j < rangeEnd; j++)
            {
                double area = Math.Abs(
                    (ax - avgX) * ((double)data[j].Value - ay) -
                    (ax - data[j].Timestamp.Ticks) * (avgY - ay)) * 0.5;
                if (area > maxArea) { maxArea = area; maxIdx = j; }
            }

            sampled.Add(data[maxIdx]);
            a = maxIdx;
        }

        sampled.Add(data[^1]);
        return sampled;
    }
}

// ── DTOs ──────────────────────────────────────────────────────────────────

public sealed class TrendablePointDto
{
    public int PointConfigId { get; init; }
    public int DeviceConfigId { get; init; }
    public string DeviceName { get; init; } = "";
    public string Key { get; init; } = "";
    public string Label { get; init; } = "";
    public string Unit { get; init; } = "";

    /// <summary>Display string for the picker list.</summary>
    public string DisplayName => string.IsNullOrWhiteSpace(Unit)
        ? $"{DeviceName} — {Label}"
        : $"{DeviceName} — {Label} ({Unit})";
}

public sealed class TrendPointDto
{
    public DateTime Timestamp { get; init; }
    public decimal Value { get; init; }
}
