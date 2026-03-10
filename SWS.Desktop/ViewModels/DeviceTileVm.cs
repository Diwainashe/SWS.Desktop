using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace SWS.Desktop.ViewModels;

/// <summary>
/// UI model for one device tile on the dashboard.
/// Keeps the dashboard layout simple and avoids binding directly to snapshots.
/// </summary>
public sealed partial class DeviceTileVm : ObservableObject
{
    public int DeviceId { get; init; }
    public string DeviceName { get; init; } = "";

    [ObservableProperty] private string _weight = "—";
    [ObservableProperty] private string _flowrate = "—";
    [ObservableProperty] private string _runState = "—";
    [ObservableProperty] private string _alarmSummary = "OK";

    // Optional: show comm quality quickly
    [ObservableProperty] private string _quality = "—";
}