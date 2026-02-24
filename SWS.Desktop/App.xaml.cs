using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using SWS.Acquisition;
using SWS.Core.Abstractions;
using SWS.Data;
using SWS.Desktop.Services;
using SWS.Desktop.ViewModels;
using SWS.Desktop.Views;
using SWS.Modbus;
using System.Windows;

namespace SWS.Desktop;

public partial class App : Application
{
    private IHost? _host;

    protected override async void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        if (!TryEnsureDatabaseReady(out var errorMessage))
        {
            MessageBox.Show(errorMessage, "SWS", MessageBoxButton.OK, MessageBoxImage.Error);
            Shutdown();
            return;
        }

        _host = Host.CreateDefaultBuilder()
            .ConfigureServices(services =>
            {
                var cs = global::SWS.Desktop.Properties.Settings.Default.SwsConnectionString;

                // EF: Factory is correct for desktop apps
                services.AddDbContextFactory<SwsDbContext>(options =>
                    options.UseSqlServer(cs));

                // Modbus
                services.AddSingleton<IModbusClient, NModbusClient>();

                // Acquisition + data services (scoped is fine; navigation will create scopes)
                services.AddScoped<DevicePollerService>();
                services.AddScoped<SmokeReadOnceService>();
                services.AddScoped<ConfigDataService>();

                // Background poller
                services.AddHostedService<PollingHostedService>();

                // Navigation + Shell
                services.AddSingleton<INavigationService, AppNavigationService>();
                services.AddSingleton<MainShellViewModel>();

                // Main window (single window app)
                services.AddSingleton<MainWindow>();

                // Pages + Page VMs MUST be transient (created per navigation scope)
                services.AddTransient<DashboardPageViewModel>();
                services.AddTransient<DevicesViewModel>();
                services.AddTransient<PointsViewModel>();

                services.AddTransient<DashboardView>();
                services.AddTransient<DevicesView>();
                services.AddTransient<PointsView>();

                // Add later when you create it:
                // services.AddTransient<ConfigViewModel>();
                // services.AddTransient<ConfigView>();
            })
            .Build();

        await _host.StartAsync(); // ✅ starts BackgroundService

        // Show ONE window only (the shell)
        var main = _host.Services.GetRequiredService<MainWindow>();
        main.Show();
    }

    protected override async void OnExit(ExitEventArgs e)
    {
        if (_host != null)
            await _host.StopAsync();

        _host?.Dispose();
        base.OnExit(e);
    }

    // Keep your existing DB setup + dialog logic:
    private static bool TryEnsureDatabaseReady(out string errorMessage)
    {
        // ... your existing implementation ...
        errorMessage = string.Empty;
        return true;
    }
}