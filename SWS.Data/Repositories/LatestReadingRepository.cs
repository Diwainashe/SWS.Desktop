using Microsoft.EntityFrameworkCore;
using SWS.Core.Models;

namespace SWS.Data.Repositories;

/// <summary>
/// Small repository to upsert the latest reading.
/// Keeps DB writing logic out of the polling engine.
/// </summary>
public sealed class LatestReadingRepository
{
    private readonly SwsDbContext _db;

    public LatestReadingRepository(SwsDbContext db)
    {
        _db = db;
    }

    public async Task UpsertAsync(LatestReading latest, CancellationToken ct)
    {
        var existing = await _db.LatestReadings.FindAsync(
            new object[] { latest.DeviceConfigId, latest.PointConfigId }, ct);

        if (existing is null)
        {
            _db.LatestReadings.Add(latest);
        }
        else
        {
            existing.TimestampLocal = latest.TimestampLocal;
            existing.ValueNumeric = latest.ValueNumeric;
            existing.ErrorText = latest.ErrorText;
            existing.Quality = latest.Quality;
            existing.UpdatedLocal = DateTime.UtcNow;
        }

        await _db.SaveChangesAsync(ct);
    }

    public Task<List<LatestReading>> GetAllAsync(CancellationToken ct)
        => _db.LatestReadings.AsNoTracking().ToListAsync(ct);
}
