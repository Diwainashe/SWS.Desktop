using Microsoft.EntityFrameworkCore;
using SWS.Core.Abstractions;
using SWS.Core.Models;
using SWS.Data;
using SWS.Modbus;
using System.Collections.Concurrent;
using System.Net.Sockets;

namespace SWS.Acquisition;

/// <summary>
/// Polls configured devices/points and writes:
/// - LatestReadings (one row per device+point for fast UI)
/// - ReadingHistories (append-only for trending) depending on policy
///
/// Rules:
/// - Only polls configured points (Address > 0)
/// - Respects PollRateMs per point
/// - Stores numeric only for good reads; stores ErrorText only for errors
/// </summary>
public sealed class DevicePollerService
{
    private readonly SwsDbContext _db;
    private readonly IModbusClient _modbus;

    // Per point (DeviceId:PointId) next allowed poll timestamp
    private static readonly ConcurrentDictionary<string, DateTime> _nextPollUtc = new();

    public DevicePollerService(SwsDbContext db, IModbusClient modbus)
    {
        _db = db;
        _modbus = modbus;
    }

    /// <summary>
    /// One polling cycle. Called repeatedly by PollingHostedService.
    /// </summary>
    public async Task PollOnceAsync(CancellationToken ct)
    {
        var nowUtc = DateTime.UtcNow;

        var devices = await _db.DeviceConfigs
            .AsNoTracking()
            .Where(d => d.IsEnabled)
            .ToListAsync(ct);

        foreach (var device in devices)
        {
            var points = await _db.PointConfigs
                .AsNoTracking()
                .Where(p => p.DeviceConfigId == device.Id)
                .ToListAsync(ct);

            foreach (var point in points)
            {
                // Only poll configured points
                if (point.Address <= 0)
                    continue;

                // Respect PollRateMs per point
                if (!IsPollDue(device.Id, point.Id, point.PollRateMs, nowUtc))
                    continue;

                var result = await ReadPointAsync(device, point, ct);

                await UpsertLatestAsync(device.Id, point.Id, nowUtc, result, ct);

                if (point.LogToHistory)
                    await TryAppendHistoryAsync(device.Id, point.Id, nowUtc, point.HistoryIntervalMs, result, ct);
            }
        }

        await _db.SaveChangesAsync(ct);
    }

    private static bool IsPollDue(int deviceId, int pointId, int pollRateMs, DateTime nowUtc)
    {
        if (pollRateMs <= 0)
            pollRateMs = 500;

        string key = $"{deviceId}:{pointId}";

        if (_nextPollUtc.TryGetValue(key, out var dueUtc) && nowUtc < dueUtc)
            return false;

        _nextPollUtc[key] = nowUtc.AddMilliseconds(pollRateMs);
        return true;
    }

    /// <summary>
    /// Reads a single point based on PointConfig settings.
    /// MVP: only HoldingRegister supported until we extend IModbusClient.
    /// </summary>
    // File: SWS.Acquisition/DevicePollerService.cs
    private async Task<ReadResult> ReadPointAsync(DeviceConfig device, PointConfig point, CancellationToken ct)
    {
        try
        {
            // MVP: skip unconfigured
            if (point.Address <= 0)
                return ReadResult.Error(ReadingQuality.BadData, "Point not configured (Address <= 0).");

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
                        // Coils are bits. Force Bool behavior:
                        // - length must be 1 for a single coil point
                        // - numeric stored as 0/1 for charts and simple UI
                        ushort len = point.Length == 0 ? (ushort)1 : point.Length;

                        bool[] bits = await _modbus.ReadCoilsAsync(device, point.Address, len, ct);

                        // For your UI/DB model, treat first bit as this point’s value.
                        bool value = bits.Length > 0 && bits[0];
                        return ReadResult.Ok(value ? 1m : 0m);
                    }

                case ModbusPointArea.DiscreteInput:
                    {
                        ushort len = point.Length == 0 ? (ushort)1 : point.Length;

                        bool[] bits = await _modbus.ReadDiscreteInputsAsync(device, point.Address, len, ct);

                        bool value = bits.Length > 0 && bits[0];
                        return ReadResult.Ok(value ? 1m : 0m);
                    }

                default:
                    return ReadResult.Error(ReadingQuality.BadData, $"Unknown Area '{point.Area}'.");
            }
        }
        catch (OperationCanceledException) { throw; }
        catch (SocketException ex) { return ReadResult.Error(ReadingQuality.Timeout, ex.Message); }
        catch (Exception ex) { return ReadResult.Error(ReadingQuality.Exception, ex.Message); }
    }

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
            existing.ValueNumeric = result.ValueNumeric;
            existing.ErrorText = null;
        }
        else
        {
            existing.ValueNumeric = null;
            existing.ErrorText = result.ErrorText;
        }
    }

    private async Task TryAppendHistoryAsync(
        int deviceId,
        int pointId,
        DateTime nowUtc,
        int historyIntervalMs,
        ReadResult result,
        CancellationToken ct)
    {
        if (historyIntervalMs <= 0)
            return;

        var last = await _db.ReadingHistories
            .AsNoTracking()
            .Where(h => h.DeviceConfigId == deviceId && h.PointConfigId == pointId)
            .OrderByDescending(h => h.TimestampUtc)
            .Select(h => (DateTime?)h.TimestampUtc)
            .FirstOrDefaultAsync(ct);

        if (last.HasValue && (nowUtc - last.Value).TotalMilliseconds < historyIntervalMs)
            return;

        _db.ReadingHistories.Add(new ReadingHistory
        {
            DeviceConfigId = deviceId,
            PointConfigId = pointId,
            TimestampUtc = nowUtc,
            Quality = result.Quality,
            ValueNumeric = (result.Quality == ReadingQuality.Good) ? result.ValueNumeric : null,
            ErrorText = (result.Quality == ReadingQuality.Good) ? null : result.ErrorText
        });
    }

    private readonly record struct ReadResult(decimal? ValueNumeric, ReadingQuality Quality, string? ErrorText)
    {
        public static ReadResult Ok(decimal value) => new(value, ReadingQuality.Good, null);
        public static ReadResult Error(ReadingQuality quality, string message) => new(null, quality, message);
    }
}