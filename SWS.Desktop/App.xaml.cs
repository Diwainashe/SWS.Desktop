using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using SWS.Acquisition;
using SWS.Core.Abstractions;
using SWS.Core.Profiles;
using SWS.Core.Services;
using SWS.Data;
using SWS.Data.Seed;
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
                // EF: also register the scoped context because DevicePollerService injects SwsDbContext directly
                services.AddDbContext<SwsDbContext>(options =>
                    options.UseSqlServer(cs));
                //Config
                services.AddSingleton<AppSettingsService>();
                services.AddSingleton<AppThemeService>();
                //Time Service
                services.AddSingleton<SWS.Core.Abstractions.ITimeProvider, SWS.Core.Abstractions.SouthAfricaTimeProvider>();

                // Modbus
                services.AddSingleton<IModbusClient, NModbusClient>();
                services.AddSingleton<IDecoder, BasicDecoder>();

                // Acquisition + data services (scoped is fine; navigation will create scopes)
                services.AddScoped<DevicePollerService>();
                services.AddScoped<SmokeReadOnceService>();
                services.AddScoped<ConfigDataService>();

                // Background poller
                services.AddHostedService<PollingHostedService>();
                services.AddSingleton<SWS.Desktop.Services.LatestReadingsBus>();
                services.AddSingleton<SWS.Core.Services.ILatestReadingsBus>(sp => sp.GetRequiredService<SWS.Desktop.Services.LatestReadingsBus>());

                // Profiles (device-specific templates + formatting)
                // register individual profiles
                services.AddSingleton<IDeviceProfile, GenericProfile>();
                services.AddSingleton<IDeviceProfile, Gm9907L5Profile>();
                services.AddSingleton<DeviceProfileRegistry>();

                // Navigation + Shell
                services.AddSingleton<INavigationService, AppNavigationService>();
                services.AddSingleton<MainShellViewModel>();

                // Main window (single window app)
                services.AddSingleton<MainWindow>();

                // Pages + Page VMs MUST be transient (created per navigation scope)
                services.AddTransient<DashboardViewModel>();
                services.AddTransient<DevicesViewModel>();
                services.AddTransient<PointsViewModel>();
                services.AddTransient<SettingsViewModel>();

                services.AddTransient<DashboardView>();
                services.AddTransient<DevicesView>();
                services.AddTransient<PointsView>();
                services.AddTransient<SettingsView>();

                // Add later when you create it:
                // services.AddTransient<ConfigViewModel>();
                // services.AddTransient<ConfigView>();
            })
            .Build();


        // ----------------------------
        // Seed PointTemplates (once)
        // ----------------------------
        using (var scope = _host.Services.CreateScope())
        {
            // Use the DbContextFactory because it's safe for desktop apps
            var dbFactory = scope.ServiceProvider.GetRequiredService<IDbContextFactory<SwsDbContext>>();
            await using var db = await dbFactory.CreateDbContextAsync();

            // This is safe to call on every startup (it checks if empty)
            PointTemplateSeeder.SeedIfEmpty(db);
        }

        var settings = _host.Services.GetRequiredService<AppSettingsService>();
        var themeSvc = _host.Services.GetRequiredService<AppThemeService>();
        themeSvc.ApplyTheme(settings.Theme);

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