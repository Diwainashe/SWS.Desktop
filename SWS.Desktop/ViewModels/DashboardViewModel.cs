using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SWS.Core.Models;
using SWS.Core.Profiles;
using SWS.Desktop.Services;
using System.Collections.ObjectModel;

namespace SWS.Desktop.ViewModels;

/// <summary>
/// Dashboard VM:
/// - pulls latest readings from DB
/// - formats display via device profiles
/// - auto-refreshes when the poller publishes LatestUpdated
/// </summary>
public partial class DashboardViewModel : ObservableObject, IDisposable
{
    private readonly ConfigDataService _data;
    private readonly DeviceProfileRegistry _profiles;
    private readonly LatestReadingsBus _bus;   // ✅ store it so we can unsubscribe

    public ObservableCollection<LiveRowVm> Rows { get; } = new();

    [ObservableProperty] private string _status = "Ready.";
    [ObservableProperty] private bool _autoRefresh = true;

    public DashboardViewModel(
        ConfigDataService data,
        DeviceProfileRegistry profiles,
        LatestReadingsBus bus)
    {
        _data = data;
        _profiles = profiles;
        _bus = bus; // ✅ fixes CS8618 warning

        _bus.LatestUpdated += OnLatestUpdated;

        _ = RefreshAsync();
    }

    private async void OnLatestUpdated(object? sender, LatestReadingsUpdatedEventArgs e)
    {
        if (!AutoRefresh)
            return;

        await RefreshAsync();
    }

    [RelayCommand]
    private async Task RefreshAsync()
    {
        var list = await _data.GetLatestReadingsAsync(CancellationToken.None);

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

    /// <summary>
    /// Prevent event handler leaks when VM is discarded.
    /// </summary>
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