using System;
using System.Linq;
using System.Net.Sockets;
using Microsoft.EntityFrameworkCore;
using NModbus;
using SWS.Core.Abstractions;
using SWS.Core.Models;
using SWS.Data;
using SWS.Modbus;

namespace SWS.Acquisition;

public sealed class SmokeReadOnceService
{
    private readonly SwsDbContext _db;
    private readonly ITimeProvider _time;

    public SmokeReadOnceService(SwsDbContext db, ITimeProvider time)
    {
        _db = db;
        _time = time;
    }

    public string ReadOnceAndUpsertLatest()
    {
        var device = _db.DeviceConfigs.AsNoTracking()
            .FirstOrDefault(d => d.IsEnabled);

        if (device is null)
            return "No enabled device found (DeviceConfigs table is empty or disabled).";

        // For smoke test, just pick the first point for this device.
        // (Not hardcoded addresses; it reads from DB.)
        var point = _db.PointConfigs.AsNoTracking()
            .FirstOrDefault(p => p.DeviceConfigId == device.Id);

        if (point is null)
            return "No points found for the device (PointConfigs table is empty).";

        try
        {
            decimal? numeric = null;

            // ---- Read from device based on Area (Holding/Input/Coil/Discrete) ----
            using var client = new TcpClient();
            client.Connect(device.IpAddress, device.Port);

            var factory = new ModbusFactory();
            var master = factory.CreateMaster(client);

            switch (point.Area)
            {
                case ModbusPointArea.HoldingRegister:
                    {
                        int offset = ModbusAddressing.HoldingToOffset(point.Address);
                        if (offset < 0 || offset > ushort.MaxValue)
                            return $"Point address {point.Address} produced invalid offset {offset}.";

                        ushort[] regs = master.ReadHoldingRegisters(device.UnitId, (ushort)offset, point.Length);
                        numeric = ModbusDecoder.DecodeToNumeric(regs, point);
                        break;
                    }

                case ModbusPointArea.InputRegister:
                    {
                        int offset = ModbusAddressing.InputToOffset(point.Address);
                        if (offset < 0 || offset > ushort.MaxValue)
                            return $"Point address {point.Address} produced invalid offset {offset}.";

                        ushort[] regs = master.ReadInputRegisters(device.UnitId, (ushort)offset, point.Length);
                        numeric = ModbusDecoder.DecodeToNumeric(regs, point);
                        break;
                    }

                case ModbusPointArea.Coil:
                    {
                        int offset = ModbusAddressing.CoilToOffset(point.Address);
                        if (offset < 0 || offset > ushort.MaxValue)
                            return $"Point address {point.Address} produced invalid offset {offset}.";

                        bool[] bits = master.ReadCoils(device.UnitId, (ushort)offset, 1);
                        numeric = bits.Length > 0 ? (bits[0] ? 1m : 0m) : null;
                        break;
                    }

                case ModbusPointArea.DiscreteInput:
                    {
                        int offset = ModbusAddressing.DiscreteToOffset(point.Address);
                        if (offset < 0 || offset > ushort.MaxValue)
                            return $"Point address {point.Address} produced invalid offset {offset}.";

                        bool[] bits = master.ReadInputs(device.UnitId, (ushort)offset, 1);
                        numeric = bits.Length > 0 ? (bits[0] ? 1m : 0m) : null;
                        break;
                    }

                default:
                    return $"Unsupported Modbus area: {point.Area}";
            }

            UpsertLatestSuccess(device.Id, point.Id, numeric);

            // Optional historian write (controlled by DB flags)
            MaybeInsertHistory(device.Id, point, numeric, ReadingQuality.Good, errorText: null);

            return $"OK: {device.Name} {point.Key} @ {point.Address} [{point.Area}] = {(numeric?.ToString() ?? "null")}";
        }
        catch (SocketException ex)
        {
            UpsertLatestError(device.Id, point.Id, ReadingQuality.Timeout, ex.Message);
            MaybeInsertHistory(device.Id, point, null, ReadingQuality.Timeout, ex.Message);
            return $"TIMEOUT/CONNECT ERROR: {ex.Message}";
        }
        catch (Exception ex)
        {
            UpsertLatestError(device.Id, point.Id, ReadingQuality.Exception, ex.Message);
            MaybeInsertHistory(device.Id, point, null, ReadingQuality.Exception, ex.Message);
            return $"EXCEPTION: {ex.Message}";
        }
    }

    private void UpsertLatestSuccess(int deviceId, int pointId, decimal? valueNumeric)
    {
        var nowLocal = _time.NowLocal;

        var row = _db.LatestReadings
            .FirstOrDefault(r => r.DeviceConfigId == deviceId && r.PointConfigId == pointId);

        if (row is null)
        {
            row = new LatestReading
            {
                DeviceConfigId = deviceId,
                PointConfigId = pointId
            };
            _db.LatestReadings.Add(row);
        }

        row.TimestampLocal = nowLocal;
        row.ValueNumeric = valueNumeric;

        // IMPORTANT: do not store text for normal readings
        row.ErrorText = null;

        row.Quality = ReadingQuality.Good;
        row.UpdatedLocal = nowLocal;

        _db.SaveChanges();
    }

    private void UpsertLatestError(int deviceId, int pointId, ReadingQuality quality, string message)
    {
        var nowLocal = _time.NowLocal;

        var row = _db.LatestReadings
            .FirstOrDefault(r => r.DeviceConfigId == deviceId && r.PointConfigId == pointId);

        if (row is null)
        {
            row = new LatestReading
            {
                DeviceConfigId = deviceId,
                PointConfigId = pointId
            };
            _db.LatestReadings.Add(row);
        }

        row.TimestampLocal = nowLocal;
        row.ValueNumeric = null;

        // Only errors get text
        row.ErrorText = message;

        row.Quality = quality;
        row.UpdatedLocal = nowLocal;

        _db.SaveChanges();
    }

    private void MaybeInsertHistory(
        int deviceId,
        PointConfig point,
        decimal? valueNumeric,
        ReadingQuality quality,
        string? errorText)
    {
        if (!point.LogToHistory)
            return;

        // Guard bad configuration
        if (point.HistoryIntervalMs <= 0)
            return;

        var nowLocal = _time.NowLocal;
        var minTimeUtc = nowLocal.AddMilliseconds(-point.HistoryIntervalMs);

        // Simple throttling: only write if last history record is older than interval
        var last = _db.ReadingHistories.AsNoTracking()
            .Where(h => h.DeviceConfigId == deviceId && h.PointConfigId == point.Id)
            .OrderByDescending(h => h.TimestampLocal)
            .Select(h => (DateTime?)h.TimestampLocal)
            .FirstOrDefault();

        if (last.HasValue && last.Value > minTimeUtc)
            return;

        var history = new ReadingHistory
        {
            DeviceConfigId = deviceId,
            PointConfigId = point.Id,
            TimestampLocal = nowLocal,
            ValueNumeric = valueNumeric,
            Quality = quality,
            ErrorText = quality == ReadingQuality.Good ? null : errorText
        };

        _db.ReadingHistories.Add(history);
        _db.SaveChanges();
    }
}