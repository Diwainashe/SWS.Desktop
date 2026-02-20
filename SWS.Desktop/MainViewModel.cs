using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.DependencyInjection;
using SWS.Acquisition;

namespace SWS.Desktop;

/// <summary>
/// Minimal UI for smoke-testing reads while the poller runs in background.
/// Later this becomes the tile dashboard view model.
/// </summary>
public partial class MainViewModel : ObservableObject
{
    private readonly IServiceScopeFactory _scopeFactory;

    [ObservableProperty] private string _outputText = "Ready.\n";
    [ObservableProperty] private string _statusText = "Idle";

    public MainViewModel(IServiceScopeFactory scopeFactory)
    {
        _scopeFactory = scopeFactory;
    }

    [RelayCommand]
    private async Task TestReadAsync()
    {
        StatusText = "Reading...";

        try
        {
            // Create a DI scope so we get a fresh DbContext each call
            using var scope = _scopeFactory.CreateScope();

            // Resolve the smoke service from DI
            var runner = scope.ServiceProvider.GetRequiredService<SmokeReadOnceService>();

            // Run sync Modbus read off the UI thread
            string result = await Task.Run(() => runner.ReadOnceAndUpsertLatest());

            OutputText = result + "\n" + OutputText;
            StatusText = "OK";
        }
        catch (Exception ex)
        {
            OutputText = $"ERROR: {ex.Message}\n{ex}\n\n" + OutputText;
            StatusText = "Error";
        }
    }

}
