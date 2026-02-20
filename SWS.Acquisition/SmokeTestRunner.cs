using System;
using System.Linq;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using NModbus;
using SWS.Core.Models;
using SWS.Data;

namespace SWS.Acquisition
{
    /// <summary>
    /// Smoke test runner:
    /// - Pulls DeviceConfig + PointConfig from DB
    /// - Reads Modbus holding register(s)
    /// - Decodes using PointDataType
    /// - Upserts LatestReadings (1 row per device+point)
    ///
    /// This is used by the UI button while we build the full poller.
    /// </summary>
    public sealed class SmokeTestRunner
    {
        private readonly SwsDbContext _db;

        public SmokeTestRunner(SwsDbContext db)
        {
            _db = db;
        }

        /// <summary>
        /// Reads a single configured point (prefers IsEssential points).
        /// If deviceId is null, uses the first enabled device.
        /// </summary>
        public async Task<string> ReadPointAndUpsertLatestAsync(int? deviceId, CancellationToken ct)
        {
            // 1) Pick device
            var device = await _db.DeviceConfigs.AsNoTracking()
                .Where(d => d.IsEnabled)
                .Where(d => deviceId == null || d.Id == deviceId.Value)
                .OrderBy(d => d.Id)
                .FirstOrDefaultAsync(ct);

            if (device is null)
                return "No enabled DeviceConfig found.";

            // 2) Pick point (prefer IsEssential=true)
            var point = await _db.PointConfigs.AsNoTracking()
                .Where(p => p.DeviceConfigId == device.Id)
                .OrderByDescending(p => p.IsEssential)
                .ThenBy(p => p.Id)
                .FirstOrDefaultAsync(ct);

            if (point is null)
                return $"No PointConfig found for device '{device.Name}' (Id={device.Id}).";

            // 3) Convert ModScan-style holding address 400007 -> 0-based start address 6
            // Rule: 400001 => 0
            int startOffset = point.Address - 400001;
            if (startOffset < 0)
                return $"PointConfig.Address {point.Address} is invalid for holding register mapping (expected >= 400001).";

            try
            {
                ct.ThrowIfCancellationRequested();

                // 4) Connect + read
                using var tcp = new TcpClient();
                await tcp.ConnectAsync(device.IpAddress, device.Port, ct);

                var factory = new ModbusFactory();
                var master = factory.CreateMaster(tcp);

                // NModbus ReadHoldingRegisters is sync; keep it inside Task.Run to avoid UI thread blocking.
                ushort[] regs = await Task.Run(() =>
                    master.ReadHoldingRegisters(device.UnitId, (ushort)startOffset, point.Length),
                    ct);

                // 5) Decode to numeric + text
                (decimal? numeric, string text) = Decode(regs, point);

                // 6) Upsert LatestReading
                var now = DateTime.UtcNow;

                var existing = await _db.LatestReadings
                    .FirstOrDefaultAsync(r => r.DeviceConfigId == device.Id && r.PointConfigId == point.Id, ct);

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

                await _db.SaveChangesAsync(ct);

                return $"OK: {device.Name} | {point.Key} | HR {point.Address} (offset {startOffset}) = {text}";
            }
            catch (SocketException ex)
            {
                await UpsertErrorAsync(device.Id, point.Id, ReadingQuality.Timeout, ex.Message, ct);
                return $"TIMEOUT/CONNECT ERROR: {ex.Message}";
            }
            catch (Exception ex)
            {
                await UpsertErrorAsync(device.Id, point.Id, ReadingQuality.Exception, ex.Message, ct);
                return $"EXCEPTION: {ex.Message}";
            }
        }

        /// <summary>
        /// Minimal decoder for your current types. We’ll expand 32-bit + float next.
        /// </summary>
        private static (decimal? numeric, string text) Decode(ushort[] regs, PointConfig point)
        {
            if (regs.Length == 0)
                return (null, "");

            switch (point.DataType)
            {
                case PointDataType.UInt16:
                    {
                        ushort v = regs[0];
                        decimal scaled = v * point.Scale;
                        return (scaled, v.ToString());
                    }
                case PointDataType.Int16:
                    {
                        short v = unchecked((short)regs[0]); // unsigned -> signed
                        decimal scaled = v * point.Scale;
                        return (scaled, v.ToString());
                    }

                // Not needed for your current 400007 smoke test, but stubbed for later
                default:
                    return (null, string.Join(",", regs));
            }
        }

        private async Task UpsertErrorAsync(int deviceId, int pointId, ReadingQuality q, string msg, CancellationToken ct)
        {
            var now = DateTime.UtcNow;

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

            existing.TimestampUtc = now;
            existing.ValueNumeric = null;
            existing.ValueText = msg;
            existing.Quality = q;
            existing.UpdatedUtc = now;

            await _db.SaveChangesAsync(ct);
        }
    }
}
