using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.DependencyInjection;
using SWS.Acquisition;

namespace SWS.Desktop.ViewModels;

public partial class DashboardViewModel : ObservableObject
{
    private readonly IServiceScopeFactory _scopeFactory;

    [ObservableProperty] private string _outputText = "Ready.\n";
    [ObservableProperty] private string _statusText = "Idle";

    public DashboardViewModel(IServiceScopeFactory scopeFactory)
    {
        _scopeFactory = scopeFactory;
    }

    [RelayCommand]
    private async Task TestReadAsync()
    {
        StatusText = "Reading...";

        try
        {
            using var scope = _scopeFactory.CreateScope();
            var runner = scope.ServiceProvider.GetRequiredService<SmokeReadOnceService>();

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