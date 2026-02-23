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

    protected override void OnStartup(StartupEventArgs e)
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

                // ✅ IMPORTANT: Use DbContextFactory for UI apps to avoid cross-thread / concurrency issues
                services.AddDbContextFactory<SwsDbContext>(options =>
                    options.UseSqlServer(cs));

                // Modbus
                services.AddSingleton<IModbusClient, NModbusClient>();

                // Acquisition
                services.AddScoped<DevicePollerService>();
                services.AddScoped<SmokeReadOnceService>();

                // Navigation + Shell
                services.AddSingleton<INavigationService, AppNavigationService>();
                services.AddSingleton<MainWindow>();

                // ViewModels (singleton is fine when they use DbContextFactory per call)
                services.AddSingleton<MainShellViewModel>();
                services.AddSingleton<DashboardViewModel>();
                services.AddSingleton<ConfigViewModel>();

                // Views (UserControls) – created by NavigationService
                services.AddTransient<DashboardView>();
                services.AddTransient<ConfigView>();
            })
            .Build();

        // Show ONE window only (the shell)
        var main = _host.Services.GetRequiredService<MainWindow>();
        main.DataContext = _host.Services.GetRequiredService<MainShellViewModel>();
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