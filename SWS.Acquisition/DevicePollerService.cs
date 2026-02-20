using Microsoft.Extensions.DependencyInjection;
using SWS.Core.Models;
using SWS.Data;
using SWS.Modbus;
using System.Net.Sockets;

public class DevicePollerService
{
    private readonly SwsDbContext _db;
    private readonly IServiceScopeFactory _scopeFactory;

    public DevicePollerService(SwsDbContext db, IServiceScopeFactory scopeFactory)
    {
        _db = db;
        _scopeFactory = scopeFactory;
    }

    public async Task PollDevicesAsync()
    {
        var devices = _db.DeviceConfigs.Where(d => d.IsEnabled).ToList();

        foreach (var device in devices)
        {
            var points = _db.PointConfigs.Where(p => p.DeviceConfigId == device.Id).ToList();

            foreach (var point in points)
            {
                // Ensure you read and write based on PointConfig settings
                var result = await ReadPointDataAsync(device, point);

                // Upsert to LatestReadings
                var existing = _db.LatestReadings.FirstOrDefault(r => r.DeviceConfigId == device.Id && r.PointConfigId == point.Id);
                if (existing == null)
                {
                    existing = new LatestReading
                    {
                        DeviceConfigId = device.Id,
                        PointConfigId = point.Id,
                    };
                    _db.LatestReadings.Add(existing);
                }

                existing.ValueNumeric = result.ValueNumeric;
                existing.ErrorText = result.ValueText; // Store text only if error
                existing.TimestampUtc = DateTime.UtcNow;

                _db.SaveChanges();

                // If LogToHistory is true, log to ReadingHistory
                if (point.LogToHistory)
                {
                    LogToHistory(device, point, result);
                }
            }
        }
    }

    private async Task<(decimal? ValueNumeric, string ValueText)> ReadPointDataAsync(DeviceConfig device, PointConfig point)
    {
        // Create an instance of NModbusClient to call the ReadHoldingRegistersAsync method
        var modbusClient = new NModbusClient();

        // Simulate reading from the Modbus device (replace with actual reading logic)
        ushort[] registers = await modbusClient.ReadHoldingRegistersAsync(
            device,               // DeviceConfig object
            point.Address,        // Logical address
            point.Length,         // Length (number of registers to read)
            CancellationToken.None  // Passing the cancellation token
        );

        // Decode register data
        var valueNumeric = ModbusDecoder.DecodeToNumeric(registers, point);
        var valueText = ModbusDecoder.DecodeToText(registers, point);

        return (valueNumeric, valueText);
    }

    private void LogToHistory(DeviceConfig device, PointConfig point, (decimal? ValueNumeric, string ValueText) result)
    {
        if (point.HistoryIntervalMs > 0)
        {
            var historyEntry = new ReadingHistory
            {
                DeviceConfigId = device.Id,
                PointConfigId = point.Id,
                ValueNumeric = result.ValueNumeric,
                ErrorText = result.ValueText,
                TimestampUtc = DateTime.UtcNow,
                Quality = ReadingQuality.Good
            };

            _db.ReadingHistories.Add(historyEntry);
            _db.SaveChanges();
        }
    }
}