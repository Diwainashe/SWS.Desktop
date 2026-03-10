using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.CodeAnalysis;
using SWS.Core.Models;
using SWS.Core.Profiles;
using SWS.Desktop.Services;
using System.Collections.ObjectModel;
using System.Windows;

namespace SWS.Desktop.ViewModels;

/// <summary>
/// Dashboard VM:
/// - pulls latest readings from DB
/// - formats display via device profiles
/// - auto-refreshes when the poller publishes LatestUpdated
/// - exposes per-device groups for "View Detail" navigation
/// </summary>
public partial class DashboardViewModel : ObservableObject, IDisposable
{
    private readonly ConfigDataService _data;
    private readonly DeviceProfileRegistry _profiles;
    private readonly LatestReadingsBus _bus;
    private readonly Gm9907L5DiagnosticsService _gmDiag;
    private readonly TemplateDiagnosticsService _templateDiag;
    private readonly INavigationService _nav;

    public ObservableCollection<LiveRowVm> Rows { get; } = new();
    public ObservableCollection<DeviceGroupVm> DeviceGroups { get; } = new();

    [ObservableProperty] private string _status = "Ready.";
    [ObservableProperty] private bool _autoRefresh = true;
    public ObservableCollection<string> ActiveAlarms { get; } = new();
    public ObservableCollection<string> ActiveStates { get; } = new();
    public DiagnosticsVm Diagnostics { get; } = new();

    public DashboardViewModel(
        ConfigDataService data,
        DeviceProfileRegistry profiles,
        LatestReadingsBus bus,
        Gm9907L5DiagnosticsService gmDiag,
        TemplateDiagnosticsService templateDiag,
        INavigationService nav)
    {
        _data = data;
        _profiles = profiles;
        _bus = bus;
        _gmDiag = gmDiag;
        _nav = nav;
        _bus.LatestUpdated += OnLatestUpdated;
        _ = RefreshAsync();
        _templateDiag = templateDiag;
    }

    private void OnLatestUpdated(object? sender, LatestReadingsUpdatedEventArgs e)
    {
        if (!AutoRefresh)
            return;

        Application.Current.Dispatcher.InvokeAsync(async () =>
        {
            try
            {
                await RefreshAsync();
            }
            catch (Exception ex)
            {
                Status = $"Refresh error: {ex.Message}";
            }
        });
    }

    [RelayCommand]
    private async Task RefreshAsync()
    {
        var list = await _data.GetLatestReadingsAsync(CancellationToken.None);

        // Build device groups (for the "View Detail" buttons)
        DeviceGroups.Clear();
        foreach (var dg in list.GroupBy(x => x.DeviceId))
        {
            var first = dg.First();
            DeviceGroups.Add(new DeviceGroupVm
            {
                DeviceId = first.DeviceId,
                DeviceName = first.DeviceName,
                DeviceType = first.DeviceType
            });
        }

        // Pick the first GM device's context for dashboard diagnostics
        var gmRows = list
            .Where(x => x.DeviceType == DeviceType.GM9907_L5)
            .GroupBy(x => x.DeviceId)
            .Select(g => g.ToList())
            .FirstOrDefault();

        if (gmRows != null)
        {
            var result = _templateDiag.Build(DeviceType.GM9907_L5, gmRows);
            Diagnostics.SetAlarmGroups(result.AlarmGroups);
            Diagnostics.SetStateGroups(result.StateGroups);
        }
        else
        {
            Diagnostics.SetAlarmGroups(Array.Empty<(string, IEnumerable<string>)>());
            Diagnostics.SetStateGroups(Array.Empty<(string, IEnumerable<string>)>());
        }

        Rows.Clear();

        foreach (var deviceGroup in list.GroupBy(x => x.DeviceId))
        {
            var context = deviceGroup.ToList();
            var profile = _profiles.Get(context[0].DeviceType);

            foreach (var r in context)
            {
                var display = profile.FormatDisplay(r.Key, r.ValueNumeric, context);

                Rows.Add(new LiveRowVm
                {
                    DeviceName = r.DeviceName,
                    Label = r.Label,
                    Key = r.Key,
                    DisplayValue = display,
                    Quality = r.Quality.ToString(),
                    TimestampLocal = r.TimestampLocal
                });
            }
        }

        Status = $"Loaded {Rows.Count} rows.";
    }

    [RelayCommand]
    private void ToggleAutoRefresh()
    {
        AutoRefresh = !AutoRefresh;
        Status = AutoRefresh ? "Auto ON" : "Auto OFF";
    }

    [RelayCommand]
    private async Task ViewDeviceDetailAsync(DeviceGroupVm group)
    {
        await _nav.NavigateToDeviceAsync(group.DeviceId, group.DeviceName, group.DeviceType);
    }

    public void Dispose()
    {
        _bus.LatestUpdated -= OnLatestUpdated;
    }
}

public sealed class LiveRowVm
{
    public string DeviceName { get; set; } = "";
    public string Label { get; set; } = "";
    public string Key { get; set; } = "";
    public string DisplayValue { get; set; } = "";
    public string Quality { get; set; } = "";
    public DateTime TimestampLocal { get; set; }
}

public sealed class DeviceGroupVm
{
    public int DeviceId { get; set; }
    public string DeviceName { get; set; } = "";
    public DeviceType DeviceType { get; set; }
}
