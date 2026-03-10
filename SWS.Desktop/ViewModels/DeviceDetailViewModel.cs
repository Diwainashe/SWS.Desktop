using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SWS.Core.Models;
using SWS.Core.Profiles;
using SWS.Desktop.Services;
using System.Collections.ObjectModel;
using System.Windows;

namespace SWS.Desktop.ViewModels;

/// <summary>
/// Detail view for a single device.
/// Displays:
///   - Summary panel: Weight, Flowrate, Run state, Recipe (from key readings)
///   - Diagnostics: alarm groups + state groups (via TemplateDiagnosticsService)
///   - Full point list: all live readings for this device
///
/// Receives the target DeviceId before activation via SetDevice().
/// </summary>
public partial class DeviceDetailViewModel : ObservableObject, IDisposable
{
    private readonly ConfigDataService _data;
    private readonly DeviceProfileRegistry _profiles;
    private readonly LatestReadingsBus _bus;
    private readonly TemplateDiagnosticsService _templateDiag;

    private int _deviceId;
    private DeviceType _deviceType;

    // ── Summary ────────────────────────────────────────────────────────────
    [ObservableProperty] private string _deviceName = "Device";
    [ObservableProperty] private string _weightDisplay = "—";
    [ObservableProperty] private string _flowrateDisplay = "—";
    [ObservableProperty] private string _runStateDisplay = "—";
    [ObservableProperty] private string _runStateColor = "#A9B7CF";   // muted = unknown
    [ObservableProperty] private string _recipeDisplay = "—";
    [ObservableProperty] private string _status = "Loading…";
    [ObservableProperty] private bool _hasAlarms = false;

    // ── Diagnostics ────────────────────────────────────────────────────────
    public DiagnosticsVm Diagnostics { get; } = new();

    // ── Point list ─────────────────────────────────────────────────────────
    public ObservableCollection<LiveRowVm> Rows { get; } = new();

    public DeviceDetailViewModel(
        ConfigDataService data,
        DeviceProfileRegistry profiles,
        LatestReadingsBus bus,
        TemplateDiagnosticsService templateDiag)
    {
        _data = data;
        _profiles = profiles;
        _bus = bus;
        _templateDiag = templateDiag;

        _bus.LatestUpdated += OnLatestUpdated;
    }

    /// <summary>
    /// Called by the view (or navigation helper) immediately after DI construction.
    /// </summary>
    public void SetDevice(int deviceId, string deviceName, DeviceType deviceType)
    {
        _deviceId = deviceId;
        _deviceType = deviceType;
        DeviceName = deviceName;

        _ = RefreshAsync();
    }

    // ── Bus handler ────────────────────────────────────────────────────────

    private void OnLatestUpdated(object? sender, LatestReadingsUpdatedEventArgs e)
    {
        Application.Current.Dispatcher.InvokeAsync(async () =>
        {
            try { await RefreshAsync(); }
            catch (Exception ex) { Status = $"Refresh error: {ex.Message}"; }
        });
    }

    // ── Refresh ────────────────────────────────────────────────────────────

    [RelayCommand]
    private async Task RefreshAsync()
    {
        var all = await _data.GetLatestReadingsAsync(CancellationToken.None);

        // Filter to this device only
        var readings = all
            .Where(r => r.DeviceId == _deviceId)
            .ToList();

        if (readings.Count == 0)
        {
            Status = "No readings yet.";
            return;
        }

        // ── Summary panel ──────────────────────────────────────────────────
        UpdateSummary(readings);

        // ── Diagnostics ────────────────────────────────────────────────────
        var result = _templateDiag.Build(_deviceType, readings);
        Diagnostics.SetAlarmGroups(result.AlarmGroups);
        Diagnostics.SetStateGroups(result.StateGroups);

        // HasAlarms = any alarm group contains something other than "OK"
        HasAlarms = result.AlarmGroups.Any(g =>
            g.Items.Any(i => !string.Equals(i, "OK", StringComparison.OrdinalIgnoreCase)));

        // ── Point list ─────────────────────────────────────────────────────
        var profile = _profiles.Get(_deviceType);

        Rows.Clear();
        foreach (var r in readings)
        {
            Rows.Add(new LiveRowVm
            {
                DeviceName = r.DeviceName,
                Label = r.Label,
                Key = r.Key,
                DisplayValue = profile.FormatDisplay(r.Key, r.ValueNumeric, readings),
                Quality = r.Quality.ToString(),
                TimestampLocal = r.TimestampLocal
            });
        }

        Status = $"Updated {DateTime.Now:HH:mm:ss}  ·  {Rows.Count} points";
    }

    // ── Summary helpers ────────────────────────────────────────────────────

    private void UpdateSummary(List<LatestReadingSnapshot> readings)
    {
        var profile = _profiles.Get(_deviceType);

        // Weight
        var weightRow = readings.FirstOrDefault(r => r.Key == "Weight.Display");
        WeightDisplay = weightRow != null
            ? profile.FormatDisplay("Weight.Display", weightRow.ValueNumeric, readings)
            : "—";

        // Flowrate
        var flowRow = readings.FirstOrDefault(r => r.Key == "Flowrate.Actual");
        FlowrateDisplay = flowRow != null
            ? profile.FormatDisplay("Flowrate.Actual", flowRow.ValueNumeric, readings)
            : "—";

        // Run / Stop state from Operating state register (bit 0)
        var opRow = readings.FirstOrDefault(r => r.Key == "State.Operating");
        if (opRow?.ValueNumeric != null)
        {
            ushort opWord = ToU16(opRow.ValueNumeric.Value);
            bool running = (opWord & 0x0001) != 0;
            bool hasAlarmBit = (opWord & (0x1000 | 0x2000)) != 0; // OVER or UNDER

            if (hasAlarmBit)
            {
                RunStateDisplay = "ALARM";
                RunStateColor = "#FF4444";
            }
            else if (running)
            {
                RunStateDisplay = "Running";
                RunStateColor = "#2ECC71";
            }
            else
            {
                RunStateDisplay = "Stopped";
                RunStateColor = "#A9B7CF";
            }
        }
        else
        {
            RunStateDisplay = "—";
            RunStateColor = "#A9B7CF";
        }

        // Recipe ID (register 40201 → key "Recipe.ID" if polled, else omit)
        var recipeRow = readings.FirstOrDefault(r => r.Key == "Recipe.ID");
        RecipeDisplay = recipeRow?.ValueNumeric != null
            ? $"Recipe {(int)recipeRow.ValueNumeric.Value}"
            : "—";
    }

    private static ushort ToU16(decimal d)
    {
        if (d < 0) return 0;
        if (d > ushort.MaxValue) return ushort.MaxValue;
        return (ushort)d;
    }

    // ── Navigation back command ────────────────────────────────────────────

    // Expose as event so the view can wire the nav service without a
    // circular dependency on INavigationService.
    public event EventHandler? BackRequested;

    [RelayCommand]
    private void GoBack() => BackRequested?.Invoke(this, EventArgs.Empty);

    public void Dispose()
    {
        _bus.LatestUpdated -= OnLatestUpdated;
    }
}
