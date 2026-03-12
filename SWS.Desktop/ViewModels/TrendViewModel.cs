using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using LiveChartsCore;
using LiveChartsCore.Defaults;
using LiveChartsCore.SkiaSharpView;
using LiveChartsCore.SkiaSharpView.Painting;
using SkiaSharp;
using SWS.Desktop.Services;
using System.Collections.ObjectModel;

namespace SWS.Desktop.ViewModels;

/// <summary>
/// Trending page VM.
///
/// Layout concept:
///   Left panel  — point picker (all trendable points, grouped by device)
///   Right panel — chart with multiple Y-axes + time-range + loading state
///
/// Multiple series can share one Y-axis (same unit / similar range),
/// or each get their own axis (e.g. kg vs t/h vs boolean).
/// The user controls axis assignment per selected series.
/// </summary>
public partial class TrendViewModel : ObservableObject, IDisposable
{
    private readonly TrendDataService _trendData;

    // ── State ─────────────────────────────────────────────────────────────
    [ObservableProperty] private string _status = "Select points to trend.";
    [ObservableProperty] private bool _isLoading = false;

    // ── Time range ────────────────────────────────────────────────────────
    [ObservableProperty] private TimeRangeOption _selectedRange = TimeRangeOption.Last1Hour;
    [ObservableProperty] private DateTime _customFrom = DateTime.Now.AddHours(-1);
    [ObservableProperty] private DateTime _customTo = DateTime.Now;
    [ObservableProperty] private bool _isCustomRange = false;

    // ── Unit ────────────────────────────────────────────────────────
    [ObservableProperty] private string _unit = string.Empty;

    public TimeRangeOption[] RangeOptions { get; } = Enum.GetValues<TimeRangeOption>();

    partial void OnSelectedRangeChanged(TimeRangeOption value)
        => IsCustomRange = value == TimeRangeOption.Custom;

    // ── Point picker ──────────────────────────────────────────────────────
    public ObservableCollection<TrendablePointVm> AvailablePoints { get; } = new();
    public ObservableCollection<SelectedSeriesVm> SelectedSeries { get; } = new();

    // ── Chart ─────────────────────────────────────────────────────────────
    public ObservableCollection<ISeries> Series { get; } = new();
    public ObservableCollection<Axis> XAxes { get; } = new();
    public ObservableCollection<Axis> YAxes { get; } = new();

    // Colour palette — enough for 8 distinct series
    private static readonly SKColor[] Palette =
    {
        SKColor.Parse("#4E76FF"),
        SKColor.Parse("#2ECC71"),
        SKColor.Parse("#F39C12"),
        SKColor.Parse("#E74C3C"),
        SKColor.Parse("#9B59B6"),
        SKColor.Parse("#1ABC9C"),
        SKColor.Parse("#E67E22"),
        SKColor.Parse("#3498DB"),
    };

    public TrendViewModel(TrendDataService trendData)
    {
        _trendData = trendData;
        InitChart();
        _ = LoadAvailablePointsAsync();
    }

    // ── Init ──────────────────────────────────────────────────────────────

    private void InitChart()
    {
        XAxes.Add(new DateTimeAxis(TimeSpan.FromMinutes(1), d => d.ToString("HH:mm"))
        {
            Name = "Time",
            NamePaint = new SolidColorPaint(SKColor.Parse("#A9B7CF")),
            LabelsPaint = new SolidColorPaint(SKColor.Parse("#A9B7CF")),
            SeparatorsPaint = new SolidColorPaint(SKColor.Parse("#2A3A55")),
        });
    }

    // ── Load available points ─────────────────────────────────────────────

    private async Task LoadAvailablePointsAsync()
    {
        IsLoading = true;
        Status = "Loading available points…";
        try
        {
            var points = await _trendData.GetTrendablePointsAsync(CancellationToken.None);
            AvailablePoints.Clear();
            foreach (var p in points)
                AvailablePoints.Add(new TrendablePointVm(p));

            Status = points.Count == 0
                ? "No history data yet. Points must have LogToHistory enabled."
                : $"{points.Count} point{(points.Count != 1 ? "s" : "")} available.";
        }
        catch (Exception ex)
        {
            Status = $"Error loading points: {ex.Message}";
        }
        finally
        {
            IsLoading = false;
        }
    }

    // ── Add / remove series ───────────────────────────────────────────────

    [RelayCommand]
    private void AddToChart(TrendablePointVm point)
    {
        if (SelectedSeries.Any(s => s.PointConfigId == point.Dto.PointConfigId))
            return; // already added

        if (SelectedSeries.Count >= Palette.Length)
        {
            Status = "Maximum 8 series on one chart.";
            return;
        }

        // Determine a sensible default axis — reuse existing axis with same unit,
        // or create a new one
        int axisIndex = GetOrCreateAxis(point.Dto.Unit, SelectedSeries.Count);

        var seriesVm = new SelectedSeriesVm(point.Dto, Palette[SelectedSeries.Count], axisIndex, YAxes.Count);
        seriesVm.AxisIndexChanged += OnSeriesAxisIndexChanged;
        SelectedSeries.Add(seriesVm);

        _ = LoadSeriesDataAsync(seriesVm);
    }

    [RelayCommand]
    private void RemoveFromChart(SelectedSeriesVm series)
    {
        series.AxisIndexChanged -= OnSeriesAxisIndexChanged;
        SelectedSeries.Remove(series);

        // Remove the chart series
        var chartSeries = Series.FirstOrDefault(s => s.Name == series.SeriesName);
        if (chartSeries != null) Series.Remove(chartSeries);

        RebuildAxes();
        UpdateStatus();
    }

    private void OnSeriesAxisIndexChanged(SelectedSeriesVm series)
    {
        // Reassign the chart series to the new axis
        var chartSeries = Series.FirstOrDefault(s => s.Name == series.SeriesName);
        if (chartSeries is LineSeries<DateTimePoint> ls)
            ls.ScalesYAt = series.AxisIndex;

        RebuildAxes();
    }

    // ── Time range + refresh ──────────────────────────────────────────────

    [RelayCommand]
    private async Task RefreshAsync()
    {
        if (SelectedSeries.Count == 0)
        {
            Status = "No series selected.";
            return;
        }

        IsLoading = true;
        Series.Clear();

        try
        {
            var (from, to) = GetTimeWindow();
            var tasks = SelectedSeries.Select(s => LoadSeriesDataAsync(s, from, to));
            await Task.WhenAll(tasks);
            UpdateStatus();
        }
        finally
        {
            IsLoading = false;
        }
    }

    private async Task LoadSeriesDataAsync(SelectedSeriesVm seriesVm,
        DateTime? from = null, DateTime? to = null)
    {
        var (f, t) = from.HasValue ? (from.Value, to!.Value) : GetTimeWindow();

        try
        {
            var data = await _trendData.GetHistoryAsync(
                seriesVm.PointConfigId, f, t, maxPoints: 2000, CancellationToken.None);

            var points = data
                .Select(d => new DateTimePoint(d.Timestamp, (double)d.Value))
                .ToList();

            // Remove old series with same name, add fresh
            var existing = Series.FirstOrDefault(s => s.Name == seriesVm.SeriesName);
            if (existing != null) Series.Remove(existing);

            var line = new LineSeries<DateTimePoint>
            {
                Name = seriesVm.SeriesName,
                Values = new ObservableCollection<DateTimePoint>(points),
                ScalesYAt = seriesVm.AxisIndex,
                Stroke = new SolidColorPaint(seriesVm.Color) { StrokeThickness = 1.5f },
                Fill = new SolidColorPaint(seriesVm.Color.WithAlpha(20)),
                GeometrySize = 0,   // no dots — clean line
                LineSmoothness = 0, // straight segments for process data
            };

            Series.Add(line);

            seriesVm.PointCount = points.Count;
        }
        catch (Exception ex)
        {
            Status = $"Error loading {seriesVm.Label}: {ex.Message}";
        }
    }

    // ── Axis management ───────────────────────────────────────────────────

    /// <summary>
    /// Returns the index of an existing axis whose unit matches,
    /// or creates a new axis and returns its index.
    /// </summary>
    private int GetOrCreateAxis(string unit, int seriesCount)
    {
        // Try to reuse an axis with the same unit label
        for (int i = 0; i < YAxes.Count; i++)
            if (YAxes[i].Name == unit) return i;

        // Create new axis
        var color = Palette[seriesCount % Palette.Length];
        YAxes.Add(new Axis
        {
            Name = unit,
            NamePaint = new SolidColorPaint(color),
            LabelsPaint = new SolidColorPaint(color),
            SeparatorsPaint = new SolidColorPaint(SKColor.Parse("#2A3A55")),
            Position = YAxes.Count % 2 == 0
                ? LiveChartsCore.Measure.AxisPosition.Start
                : LiveChartsCore.Measure.AxisPosition.End,
        });

        return YAxes.Count - 1;
    }

    private void RebuildAxes()
    {
        // Remove Y axes that have no series assigned
        var usedIndices = SelectedSeries.Select(s => s.AxisIndex).ToHashSet();
        var toRemove = YAxes
            .Select((a, i) => (a, i))
            .Where(x => !usedIndices.Contains(x.i))
            .Select(x => x.a)
            .ToList();

        foreach (var a in toRemove)
            YAxes.Remove(a);
    }

    // ── Time window ───────────────────────────────────────────────────────

    private (DateTime from, DateTime to) GetTimeWindow()
    {
        var to = DateTime.Now;
        var from = SelectedRange switch
        {
            TimeRangeOption.Last15Min => to.AddMinutes(-15),
            TimeRangeOption.Last1Hour => to.AddHours(-1),
            TimeRangeOption.Last4Hours => to.AddHours(-4),
            TimeRangeOption.Last8Hours => to.AddHours(-8),
            TimeRangeOption.Last24Hours => to.AddHours(-24),
            TimeRangeOption.Last7Days => to.AddDays(-7),
            TimeRangeOption.Custom => CustomFrom,
            _ => to.AddHours(-1)
        };
        return IsCustomRange ? (CustomFrom, CustomTo) : (from, to);
    }

    private void UpdateStatus()
    {
        if (SelectedSeries.Count == 0) { Status = "Select points to trend."; return; }
        var total = SelectedSeries.Sum(s => s.PointCount);
        Status = $"{SelectedSeries.Count} series · {total:N0} points";
    }

    public void Dispose() { /* nothing to unsubscribe */ }
}

// ── Supporting VMs ────────────────────────────────────────────────────────

public partial class TrendablePointVm
{
    public TrendablePointDto Dto { get; }
    public string DisplayName => Dto.DisplayName;
    public string DeviceName => Dto.DeviceName;
    public string Unit => Dto.Unit;

    public TrendablePointVm(TrendablePointDto dto) => Dto = dto;
}

public partial class SelectedSeriesVm : ObservableObject
{
    public int PointConfigId { get; }
    public string Label { get; }
    public string Unit { get; }
    public string SeriesName { get; }
    public SKColor Color { get; }
    public int TotalAxes { get; }

    private int _axisIndex;
    public int AxisIndex
    {
        get => _axisIndex;
        set
        {
            if (SetProperty(ref _axisIndex, value))
                AxisIndexChanged?.Invoke(this);
        }
    }

    private int _pointCount;
    public int PointCount { get => _pointCount; set => SetProperty(ref _pointCount, value); }

    public string ColorHex => $"#{Color.Red:X2}{Color.Green:X2}{Color.Blue:X2}";

    /// <summary>Axis options available for this series (0..YAxes.Count-1).</summary>
    public int[] AxisOptions { get; }

    public event Action<SelectedSeriesVm>? AxisIndexChanged;

    public SelectedSeriesVm(TrendablePointDto dto, SKColor color, int axisIndex, int totalAxes)
    {
        PointConfigId = dto.PointConfigId;
        Label = dto.Label;
        Unit = dto.Unit;
        SeriesName = $"{dto.DeviceName} — {dto.Label}";
        Color = color;
        _axisIndex = axisIndex;
        TotalAxes = totalAxes;
        AxisOptions = Enumerable.Range(0, Math.Max(totalAxes, 1)).ToArray();
    }
}

// ── Enums ─────────────────────────────────────────────────────────────────

public enum TimeRangeOption
{
    Last15Min,
    Last1Hour,
    Last4Hours,
    Last8Hours,
    Last24Hours,
    Last7Days,
    Custom
}
