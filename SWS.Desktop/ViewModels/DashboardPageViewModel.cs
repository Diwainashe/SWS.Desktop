using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SWS.Desktop.Services;
using System.Collections.ObjectModel;
using System.Windows.Threading;
using SWS.Core.Models;

namespace SWS.Desktop.ViewModels;

public partial class DashboardPageViewModel : ObservableObject
{
    private readonly ConfigDataService _data;

    private readonly DispatcherTimer _timer;
    private CancellationTokenSource? _refreshCts;

    public ObservableCollection<LatestReadingSnapshot> Rows { get; } = new();

    [ObservableProperty] private string _status = "Idle";
    [ObservableProperty] private bool _autoRefreshEnabled = true;

    /// <summary>
    /// How often the UI pulls latest readings from DB.
    /// Keep this >= your poll loop interval so you aren’t hammering the DB.
    /// </summary>
    public TimeSpan RefreshInterval { get; } = TimeSpan.FromMilliseconds(5000);

    public DashboardPageViewModel(ConfigDataService data)
    {
        _data = data;

        _timer = new DispatcherTimer
        {
            Interval = RefreshInterval
        };
        _timer.Tick += async (_, __) =>
        {
            if (AutoRefreshEnabled)
                await RefreshAsync();
        };

        // Start immediately
        _timer.Start();

        // Do an initial load
        _ = RefreshAsync();
    }

    [RelayCommand]
    private async Task RefreshAsync()
    {
        // Cancel any in-flight refresh so we never run two DB queries at once.
        _refreshCts?.Cancel();
        _refreshCts = new CancellationTokenSource();

        Status = "Refreshing...";

        try
        {
            var list = await _data.GetLatestReadingsAsync(_refreshCts.Token);

            Rows.Clear();
            foreach (var r in list)
                Rows.Add(r);

            Status = $"Live ({DateTime.Now:T})";
        }
        catch (OperationCanceledException)
        {
            // expected if a newer refresh starts
        }
        catch (Exception ex)
        {
            Status = $"Error: {ex.Message}";
        }
    }

    [RelayCommand]
    private void ToggleAutoRefresh()
    {
        AutoRefreshEnabled = !AutoRefreshEnabled;
        Status = AutoRefreshEnabled ? "Live" : "Paused";
    }
}