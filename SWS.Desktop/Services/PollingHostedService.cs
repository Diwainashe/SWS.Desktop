using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.DependencyInjection;
using SWS.Acquisition;

namespace SWS.Desktop.Services;

public sealed class PollingHostedService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;

    public PollingHostedService(IServiceScopeFactory scopeFactory)
    {
        _scopeFactory = scopeFactory;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        // simple loop; later we can do per-device PollMs scheduling
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = _scopeFactory.CreateScope();
                var poller = scope.ServiceProvider.GetRequiredService<DevicePollerService>();

                await poller.PollOnceAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Polling error: {ex}");
            }

            await Task.Delay(500, stoppingToken); // MVP tick; your PointConfig.PollRateMs controls per point
        }
    }
}