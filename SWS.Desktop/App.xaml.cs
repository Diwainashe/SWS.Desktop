using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using SWS.Acquisition;
using SWS.Desktop.Services;
using SWS.Desktop.Views;
using System.Windows;
using Microsoft.EntityFrameworkCore;
using SWS.Data;


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

        // Build DI container AFTER DB is ready (connection string available)
        _host = Host.CreateDefaultBuilder()
            .ConfigureServices(services =>
            {
                var cs = global::SWS.Desktop.Properties.Settings.Default.SwsConnectionString;

                services.AddDbContext<SwsDbContext>(options =>
                    options.UseSqlServer(cs));

                // Register viewmodels/windows
                services.AddSingleton<MainViewModel>();
                services.AddSingleton<MainWindow>();
                services.AddTransient<SmokeReadOnceService>();

                // Add other services here later (Acquisition, Repos, etc.)
            })
            .Build();

        // Resolve MainWindow (it will receive MainViewModel via DI)
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

    // ---------------- DB Setup + Migrations ----------------

    private static bool TryEnsureDatabaseReady(out string errorMessage)
    {
        errorMessage = string.Empty;

        if (!TryEnsureConnectionString(out var connectionString, out errorMessage))
            return false;

        try
        {
            DatabaseBootstrapper.EnsureDatabaseMigrated(connectionString);
            return true;
        }
        catch (Exception ex)
        {
            var retryMsg =
                "Database setup failed.\n\n" +
                ex.Message +
                "\n\nWould you like to update the database settings and try again?";

            var choice = MessageBox.Show(retryMsg, "SWS", MessageBoxButton.YesNo, MessageBoxImage.Warning);

            if (choice != MessageBoxResult.Yes)
            {
                errorMessage = "Database was not initialized. Startup cancelled.";
                return false;
            }

            if (!ShowDbSetupDialog(out connectionString, out errorMessage))
                return false;

            try
            {
                DatabaseBootstrapper.EnsureDatabaseMigrated(connectionString);
                return true;
            }
            catch (Exception ex2)
            {
                errorMessage = "Database setup failed again:\n\n" + ex2.Message;
                return false;
            }
        }
    }

    private static bool TryEnsureConnectionString(out string connectionString, out string errorMessage)
    {
        errorMessage = string.Empty;
        connectionString = global::SWS.Desktop.Properties.Settings.Default.SwsConnectionString;

        if (!string.IsNullOrWhiteSpace(connectionString))
            return true;

        return ShowDbSetupDialog(out connectionString, out errorMessage);
    }

    private static bool ShowDbSetupDialog(out string connectionString, out string errorMessage)
    {
        errorMessage = string.Empty;
        connectionString = string.Empty;

        var setup = new DbSetupWindow();
        var ok = setup.ShowDialog() == true;

        if (!ok)
        {
            errorMessage = "Database configuration was cancelled.";
            return false;
        }

        connectionString = global::SWS.Desktop.Properties.Settings.Default.SwsConnectionString;

        if (string.IsNullOrWhiteSpace(connectionString))
        {
            errorMessage = "Database configuration did not save a connection string.";
            return false;
        }

        return true;
    }
}
