using Microsoft.EntityFrameworkCore;
using SWS.Core.Abstractions;
using SWS.Core.Models;
using SWS.Data;
using SWS.Modbus;
using System.Net.Sockets;

namespace SWS.Acquisition;

/// <summary>
/// Polls configured devices/points and writes:
/// - LatestReadings (one row per device+point for fast UI)
/// - ReadingHistories (append-only for trending) depending on policy
/// </summary>
public sealed class DevicePollerService
{
    private readonly SwsDbContext _db;
    private readonly IModbusClient _modbus;

    public DevicePollerService(SwsDbContext db, IModbusClient modbus)
    {
        _db = db;
        _modbus = modbus;
    }

    /// <summary>
    /// One polling cycle. Call this on a timer/background loop.
    /// </summary>
    public async Task PollOnceAsync(CancellationToken ct)
    {
        // Load enabled devices
        var devices = await _db.DeviceConfigs
            .AsNoTracking()
            .Where(d => d.IsEnabled)
            .ToListAsync(ct);

        foreach (var device in devices)
        {
            // Load points for this device
            var points = await _db.PointConfigs
                .AsNoTracking()
                .Where(p => p.DeviceConfigId == device.Id)
                .ToListAsync(ct);

            foreach (var point in points)
            {
                // MVP rule: skip points not configured
                // (You can later replace this with PointConfig.IsEnabled)
                if (point.Address <= 0)
                    continue;

                var nowUtc = DateTime.UtcNow;

                // Read + decode
                var readResult = await ReadPointAsync(device, point, ct);

                // Upsert LatestReadings (numeric-only for good reads)
                await UpsertLatestAsync(device.Id, point.Id, nowUtc, readResult, ct);

                // Append historian if enabled and interval allows
                if (point.LogToHistory)
                    await TryAppendHistoryAsync(device.Id, point.Id, nowUtc, point.HistoryIntervalMs, readResult, ct);
            }
        }

        // Save once per poll cycle (better than SaveChanges inside each point)
        await _db.SaveChangesAsync(ct);
    }

    /// <summary>
    /// Reads a single point according to its Modbus area + datatype + scaling.
    /// Returns numeric value + quality + optional error text.
    /// </summary>
    private async Task<ReadResult> ReadPointAsync(DeviceConfig device, PointConfig point, CancellationToken ct)
    {
        try
        {
            // Coils/DiscreteInputs are booleans; store them as 0/1 numeric.
            // (Numeric-only storage stays true, and charts can still work.)
            switch (point.Area)
            {
                case ModbusPointArea.HoldingRegister:
                    {
                        ushort[] regs = await _modbus.ReadHoldingRegistersAsync(device, point.Address, point.Length, ct);
                        var numeric = ModbusDecoder.DecodeToNumeric(regs, point);

                        if (numeric is null)
                            return ReadResult.Error(ReadingQuality.BadData, "Decode returned null.");

                        return ReadResult.Ok(numeric.Value);
                    }

                case ModbusPointArea.InputRegister:
                    {
                        ushort[] regs = await _modbus.ReadInputRegistersAsync(device, point.Address, point.Length, ct);
                        var numeric = ModbusDecoder.DecodeToNumeric(regs, point);

                        if (numeric is null)
                            return ReadResult.Error(ReadingQuality.BadData, "Decode returned null.");

                        return ReadResult.Ok(numeric.Value);
                    }

                case ModbusPointArea.Coil:
                    {
                        // For coils, Length means "number of bits".
                        bool[] bits = await _modbus.ReadCoilsAsync(device, point.Address, length: 1, ct);

                        if (bits.Length == 0)
                            return ReadResult.Error(ReadingQuality.BadData, "No coil data returned.");

                        // Store as 1 or 0
                        return ReadResult.Ok(bits[0] ? 1m : 0m);
                    }

                case ModbusPointArea.DiscreteInput:
                    {
                        bool[] bits = await _modbus.ReadDiscreteInputsAsync(device, point.Address, length: 1, ct);

                        if (bits.Length == 0)
                            return ReadResult.Error(ReadingQuality.BadData, "No discrete input data returned.");

                        return ReadResult.Ok(bits[0] ? 1m : 0m);
                    }

                default:
                    return ReadResult.Error(ReadingQuality.BadData, $"Unknown area '{point.Area}'.");
            }
        }
        catch (OperationCanceledException)
        {
            throw; // stop cleanly
        }
        catch (SocketException ex)
        {
            return ReadResult.Error(ReadingQuality.Timeout, ex.Message);
        }
        catch (Exception ex)
        {
            return ReadResult.Error(ReadingQuality.Exception, ex.Message);
        }
    }

    /// <summary>
    /// Upserts the latest reading row (one per device+point).
    /// </summary>
    private async Task UpsertLatestAsync(
        int deviceId,
        int pointId,
        DateTime nowUtc,
        ReadResult result,
        CancellationToken ct)
    {
        var existing = await _db.LatestReadings
            .FirstOrDefaultAsync(r => r.DeviceConfigId == deviceId && r.PointConfigId == pointId, ct);

        if (existing is null)
        {
            existing = new LatestReading
            {
                DeviceConfigId = deviceId,
                PointConfigId = pointId
            };
            _db.LatestReadings.Add(existing);
        }

        existing.TimestampUtc = nowUtc;
        existing.UpdatedUtc = nowUtc;
        existing.Quality = result.Quality;

        if (result.Quality == ReadingQuality.Good)
        {
            // Numeric-only for normal reads
            existing.ValueNumeric = result.ValueNumeric;
            existing.ErrorText = null;
        }
        else
        {
            // Error text only when not good
            existing.ValueNumeric = null;
            existing.ErrorText = result.ErrorText;
        }
    }

    /// <summary>
    /// Appends a history row if enough time passed since last history record.
    /// </summary>
    private async Task TryAppendHistoryAsync(
        int deviceId,
        int pointId,
        DateTime nowUtc,
        int historyIntervalMs,
        ReadResult result,
        CancellationToken ct)
    {
        // If interval is <= 0, treat as "do not log"
        if (historyIntervalMs <= 0)
            return;

        // Check last history timestamp for this point
        var last = await _db.ReadingHistories
            .AsNoTracking()
            .Where(h => h.DeviceConfigId == deviceId && h.PointConfigId == pointId)
            .OrderByDescending(h => h.TimestampUtc)
            .Select(h => (DateTime?)h.TimestampUtc)
            .FirstOrDefaultAsync(ct);

        if (last.HasValue)
        {
            var elapsedMs = (nowUtc - last.Value).TotalMilliseconds;
            if (elapsedMs < historyIntervalMs)
                return; // too soon
        }

        // Append
        var row = new ReadingHistory
        {
            DeviceConfigId = deviceId,
            PointConfigId = pointId,
            TimestampUtc = nowUtc,
            Quality = result.Quality,
            ValueNumeric = (result.Quality == ReadingQuality.Good) ? result.ValueNumeric : null,
            ErrorText = (result.Quality == ReadingQuality.Good) ? null : result.ErrorText
        };

        _db.ReadingHistories.Add(row);
    }

    /// <summary>
    /// Small internal type so we don’t pass tuples everywhere.
    /// </summary>
    private readonly record struct ReadResult(decimal? ValueNumeric, ReadingQuality Quality, string? ErrorText)
    {
        public static ReadResult Ok(decimal value) => new(value, ReadingQuality.Good, null);

        public static ReadResult Error(ReadingQuality quality, string message) => new(null, quality, message);
    }
}