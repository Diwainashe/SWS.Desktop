using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

using SWS.Data;
using SWS.Data.Repositories;
using Microsoft.Extensions.DependencyInjection;
using SWS.Core.Abstractions;
using SWS.Core.Models;


namespace SWS.Acquisition;

/// <summary>
/// Background service that polls all enabled devices and writes results to LatestReadings.
/// This is the engine behind live dashboard tiles.
/// </summary>
public sealed class DevicePollerService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<DevicePollerService> _logger;

    public DevicePollerService(IServiceScopeFactory scopeFactory, ILogger<DevicePollerService> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        // MVP loop (simple). Next iteration: per-device scheduling & read coalescing.
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = _scopeFactory.CreateScope();

                var db = scope.ServiceProvider.GetRequiredService<SwsDbContext>();
                var latestRepo = scope.ServiceProvider.GetRequiredService<LatestReadingRepository>();
                var modbus = scope.ServiceProvider.GetRequiredService<IModbusClient>();
                var decoder = scope.ServiceProvider.GetRequiredService<IDecoder>();

                var devices = await db.DeviceConfigs
                    .Where(d => d.IsEnabled)
                    .AsNoTracking()
                    .ToListAsync(stoppingToken);

                foreach (var device in devices)
                {
                    var points = await db.PointConfigs
                        .Where(p => p.Id == device.Id)
                        .AsNoTracking()
                        .ToListAsync(stoppingToken);

                    foreach (var point in points)
                    {
                        var latest = new LatestReading
                        {
                            DeviceConfigId = device.Id,
                            PointConfigId = point.Id,
                            TimestampUtc = DateTime.UtcNow,
                            UpdatedUtc = DateTime.UtcNow
                        };

                        try
                        {
                            ushort[] regs = await modbus.ReadHoldingRegistersAsync(
                                device,
                                point.Address,
                                point.Length,
                                stoppingToken);

                            decimal? val = decoder.DecodeNumeric(point, regs);

                            latest.ValueNumeric = val;
                            latest.Quality = val.HasValue ? ReadingQuality.Good : ReadingQuality.BadData;
                        }
                        catch (TaskCanceledException) when (stoppingToken.IsCancellationRequested)
                        {
                            return;
                        }
                        catch (Exception ex)
                        {
                            _logger.LogWarning(ex, "Polling failed: {Device} {Point}", device.Name, point.Key);
                            latest.Quality = ReadingQuality.Exception;
                        }

                        await latestRepo.UpsertAsync(latest, stoppingToken);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Poller loop error");
            }

            // Global delay for MVP; we’ll refine to per-point PollRateMs next.
            await Task.Delay(500, stoppingToken);
        }
    }
}
