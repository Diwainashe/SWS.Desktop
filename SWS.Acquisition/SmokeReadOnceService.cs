using System;
using System.Linq;
using System.Net.Sockets;
using Microsoft.EntityFrameworkCore;
using NModbus;
using SWS.Core.Models;
using SWS.Data;
using SWS.Modbus;

namespace SWS.Acquisition
{
    /// <summary>
    /// One-shot Modbus read that proves the full pipeline:
    /// DB config -> Modbus read -> decode -> upsert LatestReading.
    /// </summary>
    public sealed class SmokeReadOnceService
    {
        private readonly SwsDbContext _db;

        public SmokeReadOnceService(SwsDbContext db)
        {
            _db = db;
        }

        public string ReadOnceAndUpsertLatest()
        {
            var device = _db.DeviceConfigs.AsNoTracking()
                .FirstOrDefault(d => d.IsEnabled);

            if (device is null)
                return "No enabled device found (DeviceConfigs table is empty or disabled).";

            var point = _db.PointConfigs.AsNoTracking()
                .FirstOrDefault(p => p.DeviceConfigId == device.Id);

            if (point is null)
                return "No points found for the device (PointConfigs table is empty).";

            // Convert "manual style" holding register 400007 -> 0-based offset 6
            // Rule: 400001 => 0, 400002 => 1 ... so subtract 400001
            var startOffset = ModbusAddressing.HoldingRegisterToOffset(point.Address);
            if (startOffset < 0 || startOffset > ushort.MaxValue)
                return $"Point address {point.Address} produced invalid offset {startOffset}.";

            if (startOffset < 0)
                return $"Point address {point.Address} is invalid (expected 400001+ for holding registers).";

            try
            {
                // 1) Connect
                using var client = new TcpClient();
                client.Connect(device.IpAddress, device.Port);

                // 2) Read Holding Registers (4x)
                var factory = new ModbusFactory();
                var master = factory.CreateMaster(client);

                // IMPORTANT: Use positional arguments for your NModbus version
                ushort[] regs = master.ReadHoldingRegisters(
                    device.UnitId,
                    (ushort)startOffset,
                    point.Length
                );


                // 3) Decode
                var numeric = ModbusDecoder.DecodeToDecimal(regs, point);
                var text = ModbusDecoder.DecodeToText(regs, point);

                // 4) Upsert LatestReading (one row per device+point)
                var now = DateTime.UtcNow;

                var existing = _db.LatestReadings
                    .FirstOrDefault(r => r.DeviceConfigId == device.Id && r.PointConfigId == point.Id);

                if (existing is null)
                {
                    existing = new LatestReading
                    {
                        DeviceConfigId = device.Id,
                        PointConfigId = point.Id
                    };
                    _db.LatestReadings.Add(existing);
                }

                existing.TimestampUtc = now;
                existing.ValueNumeric = numeric;
                existing.ValueText = text;
                existing.Quality = ReadingQuality.Good;
                existing.UpdatedUtc = now;

                _db.SaveChanges();

                return $"OK: {device.Name} {point.Key} @ {point.Address} (offset {startOffset}) = {text}";
            }
            catch (SocketException ex)
            {
                UpsertError(device.Id, point.Id, ReadingQuality.Timeout, ex.Message);
                return $"TIMEOUT/CONNECT ERROR: {ex.Message}";
            }
            catch (Exception ex)
            {
                UpsertError(device.Id, point.Id, ReadingQuality.Exception, ex.Message);
                return $"EXCEPTION: {ex.Message}";
            }
        }

        private void UpsertError(int deviceId, int pointId, ReadingQuality quality, string message)
        {
            var now = DateTime.UtcNow;

            var existing = _db.LatestReadings
                .FirstOrDefault(r => r.DeviceConfigId == deviceId && r.PointConfigId == pointId);

            if (existing is null)
            {
                existing = new LatestReading
                {
                    DeviceConfigId = deviceId,
                    PointConfigId = pointId
                };
                _db.LatestReadings.Add(existing);
            }

            existing.TimestampUtc = now;
            existing.ValueNumeric = null;
            existing.ValueText = message;
            existing.Quality = quality;
            existing.UpdatedUtc = now;

            _db.SaveChanges();
        }
    }
}
