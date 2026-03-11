using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SWS.Core.Models;
using SWS.Core.Profiles;
using SWS.Desktop.Services;
using System.Collections.ObjectModel;
using System.Windows;

namespace SWS.Desktop.ViewModels;

/// <summary>
/// Dashboard VM — builds a tile per device with critical live data.
/// Tiles are the primary UI; clicking a tile navigates to DeviceDetailView.
/// </summary>
public partial class DashboardViewModel : ObservableObject, IDisposable
{
    private readonly ConfigDataService _data;
    private readonly DeviceProfileRegistry _profiles;
    private readonly LatestReadingsBus _bus;
    private readonly TemplateDiagnosticsService _templateDiag;
    private readonly INavigationService _nav;

    public ObservableCollection<DeviceTileVm> Tiles { get; } = new();

    [ObservableProperty] private string _status = "Ready.";
    [ObservableProperty] private bool _autoRefresh = true;

    public DashboardViewModel(
        ConfigDataService data,
        DeviceProfileRegistry profiles,
        LatestReadingsBus bus,
        Gm9907L5DiagnosticsService gmDiag,   // kept in ctor for DI compat; not used directly
        TemplateDiagnosticsService templateDiag,
        INavigationService nav)
    {
        _data = data;
        _profiles = profiles;
        _bus = bus;
        _templateDiag = templateDiag;
        _nav = nav;

        _bus.LatestUpdated += OnLatestUpdated;
        _ = RefreshAsync();
    }

    private void OnLatestUpdated(object? sender, LatestReadingsUpdatedEventArgs e)
    {
        if (!AutoRefresh) return;
        Application.Current.Dispatcher.InvokeAsync(async () =>
        {
            try { await RefreshAsync(); }
            catch (Exception ex) { Status = $"Refresh error: {ex.Message}"; }
        });
    }

    [RelayCommand]
    private async Task RefreshAsync()
    {
        var list = await _data.GetLatestReadingsAsync(CancellationToken.None);

        // Build / update one tile per device
        var incoming = list.GroupBy(x => x.DeviceId).ToList();

        // Remove tiles for devices no longer present
        var activeIds = incoming.Select(g => g.Key).ToHashSet();
        foreach (var gone in Tiles.Where(t => !activeIds.Contains(t.DeviceId)).ToList())
            Tiles.Remove(gone);

        foreach (var group in incoming)
        {
            var readings = group.ToList();
            var first = readings[0];
            var profile = _profiles.Get(first.DeviceType);

            // Derive tile data
            var opSnap = readings.FirstOrDefault(r => r.Key == "State.Operating");
            ushort opWord = opSnap?.ValueNumeric is decimal d ? ToU16(d) : (ushort)0;

            bool running = (opWord & 0x0001) != 0;
            bool hasAlarmBits = (opWord & 0x3000) != 0;

            var diagResult = _templateDiag.Build(first.DeviceType, readings);
            bool hasAlarms = diagResult.AlarmGroups.Any(g =>
                g.Items.Any(i => !string.Equals(i, "OK", StringComparison.OrdinalIgnoreCase)));
            int alarmCount = diagResult.AlarmGroups
                .SelectMany(g => g.Items)
                .Count(i => !string.Equals(i, "OK", StringComparison.OrdinalIgnoreCase));

            string weight = profile.FormatDisplay("Weight.Display", Val(readings, "Weight.Display"), readings);
            string flowrate = profile.FormatDisplay("Flowrate.Actual", Val(readings, "Flowrate.Actual"), readings);

            // Find existing tile or create new one
            var tile = Tiles.FirstOrDefault(t => t.DeviceId == first.DeviceId);
            if (tile == null)
            {
                tile = new DeviceTileVm { DeviceId = first.DeviceId };
                Tiles.Add(tile);
            }

            tile.DeviceName = first.DeviceName;
            tile.DeviceType = first.DeviceType.ToString().Replace("_", "-");
            tile.Weight = weight;
            tile.Flowrate = flowrate;
            tile.HasAlarms = hasAlarms || hasAlarmBits;
            tile.AlarmCount = alarmCount;

            (tile.RunState, tile.RunStateColor, tile.StatusColor) =
                (hasAlarms || hasAlarmBits) ? ("ALARM", "#FF4444", "#3D1010") :
                running ? ("Running", "#2ECC71", "#0D2B1A") :
                                              ("Stopped", "#A9B7CF", "#0C1626");
        }

        int tileCount = Tiles.Count;
        int alarmTiles = Tiles.Count(t => t.HasAlarms);
        Status = alarmTiles > 0
            ? $"{tileCount} device{(tileCount != 1 ? "s" : "")}  ·  {alarmTiles} in alarm"
            : $"{tileCount} device{(tileCount != 1 ? "s" : "")}  ·  All OK";
    }

    [RelayCommand]
    private void ToggleAutoRefresh()
    {
        AutoRefresh = !AutoRefresh;
        Status = AutoRefresh ? "Auto ON" : "Auto OFF";
    }

    [RelayCommand]
    private async Task OpenTileAsync(DeviceTileVm tile)
    {
        await _nav.NavigateToDeviceAsync(tile.DeviceId, tile.DeviceName,
            Enum.Parse<SWS.Core.Models.DeviceType>(tile.DeviceType.Replace("-", "_")));
    }

    private static decimal? Val(List<LatestReadingSnapshot> r, string key)
        => r.FirstOrDefault(x => x.Key == key)?.ValueNumeric;

    private static ushort ToU16(decimal d)
    {
        if (d < 0) return 0;
        if (d > ushort.MaxValue) return ushort.MaxValue;
        return (ushort)d;
    }

    public void Dispose() => _bus.LatestUpdated -= OnLatestUpdated;
}

// ── Tile VM ────────────────────────────────────────────────────────────────

public sealed class DeviceTileVm : ObservableObject
{
    public int DeviceId { get; init; }

    private string _deviceName = "";
    public string DeviceName { get => _deviceName; set => SetProperty(ref _deviceName, value); }

    private string _deviceType = "";
    public string DeviceType { get => _deviceType; set => SetProperty(ref _deviceType, value); }

    private string _weight = "—";
    public string Weight { get => _weight; set => SetProperty(ref _weight, value); }

    private string _flowrate = "—";
    public string Flowrate { get => _flowrate; set => SetProperty(ref _flowrate, value); }

    private string _runState = "—";
    public string RunState { get => _runState; set => SetProperty(ref _runState, value); }

    private string _runStateColor = "#A9B7CF";
    public string RunStateColor { get => _runStateColor; set => SetProperty(ref _runStateColor, value); }

    private string _statusColor = "#0C1626";
    public string StatusColor { get => _statusColor; set => SetProperty(ref _statusColor, value); }

    private bool _hasAlarms;
    public bool HasAlarms { get => _hasAlarms; set => SetProperty(ref _hasAlarms, value); }

    private int _alarmCount;
    public int AlarmCount { get => _alarmCount; set => SetProperty(ref _alarmCount, value); }
}

// ── Kept for backward compat (DeviceDetailView navigation) ────────────────

public sealed class LiveRowVm
{
    public string DeviceName { get; set; } = "";
    public string Label { get; set; } = "";
    public string Key { get; set; } = "";
    public string DisplayValue { get; set; } = "";
    public string Quality { get; set; } = "";
    public DateTime TimestampLocal { get; set; }
}
