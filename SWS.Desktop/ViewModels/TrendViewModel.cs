using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using LiveChartsCore;
using LiveChartsCore.Defaults;
using LiveChartsCore.SkiaSharpView;
using LiveChartsCore.SkiaSharpView.Painting;
using SkiaSharp;
using SWS.Desktop.Services;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows.Data;

namespace SWS.Desktop.ViewModels;

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

    public TimeRangeOption[] RangeOptions { get; } = Enum.GetValues<TimeRangeOption>();

    partial void OnSelectedRangeChanged(TimeRangeOption value)
        => IsCustomRange = value == TimeRangeOption.Custom;

    // ── Point picker ──────────────────────────────────────────────────────
    public ObservableCollection<TrendablePointVm> AvailablePoints { get; } = new();
    public ICollectionView AvailablePointsView { get; }
    public ObservableCollection<SelectedSeriesVm> SelectedSeries { get; } = new();

    // ── Chart — array properties so PropertyChanged triggers LiveCharts redraw ──
    [ObservableProperty] private ISeries[] _series = Array.Empty<ISeries>();
    [ObservableProperty] private Axis[] _xAxes = Array.Empty<Axis>();
    [ObservableProperty] private Axis[] _yAxes = Array.Empty<Axis>();

    public bool HasSeries => Series.Length > 0;

    partial void OnSeriesChanged(ISeries[] value)
        => OnPropertyChanged(nameof(HasSeries));

    // Colour palette
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

        // Wire up grouped view for the point picker
        var cvs = new CollectionViewSource { Source = AvailablePoints };
        cvs.GroupDescriptions.Add(new PropertyGroupDescription(nameof(TrendablePointVm.DeviceName)));
        AvailablePointsView = cvs.View;

        InitChart();
        _ = LoadAvailablePointsAsync();
    }

    // ── Init ──────────────────────────────────────────────────────────────

    private void InitChart()
    {
        XAxes = BuildXAxis(TimeSpan.FromHours(1));
        YAxes = new[]
        {
            new Axis
            {
                LabelsPaint = new SolidColorPaint(SKColor.Parse("#A9B7CF")),
                SeparatorsPaint = new SolidColorPaint(SKColor.Parse("#2A3A55")),
            }
        };
    }

    private static Axis[] BuildXAxis(TimeSpan span)
    {
        var tickUnit = span.TotalHours <= 1 ? TimeSpan.FromMinutes(5)
                     : span.TotalHours <= 8 ? TimeSpan.FromMinutes(30)
                     : span.TotalHours <= 24 ? TimeSpan.FromHours(2)
                     : TimeSpan.FromHours(12);

        return new[]
        {
            new DateTimeAxis(tickUnit, d =>
                span.TotalHours <= 24 ? d.ToString("HH:mm") : d.ToString("dd/MM HH:mm"))
            {
                Name = "Time",
                NamePaint = new SolidColorPaint(SKColor.Parse("#A9B7CF")),
                LabelsPaint = new SolidColorPaint(SKColor.Parse("#A9B7CF")),
                SeparatorsPaint = new SolidColorPaint(SKColor.Parse("#2A3A55")),
            }
        };
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
                ? "No history data yet. Enable LogToHistory on points first."
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
        if (SelectedSeries.Any(s => s.PointConfigId == point.Dto.PointConfigId)) return;
        if (SelectedSeries.Count >= Palette.Length) { Status = "Maximum 8 series."; return; }

        var seriesVm = new SelectedSeriesVm(point.Dto, Palette[SelectedSeries.Count]);
        SelectedSeries.Add(seriesVm);
        Status = $"{SelectedSeries.Count} series selected — press Load.";
    }

    [RelayCommand]
    private void RemoveFromChart(SelectedSeriesVm series)
    {
        SelectedSeries.Remove(series);
        UpdateStatus();
    }

    // ── Refresh ───────────────────────────────────────────────────────────

    [RelayCommand]
    private async Task RefreshAsync()
    {
        if (SelectedSeries.Count == 0) { Status = "No series selected."; return; }

        IsLoading = true;
        Status = "Loading…";

        try
        {
            var (from, to) = GetTimeWindow();

            // Build axes based on the actual time window
            var newXAxes = BuildXAxis(to - from);
            var newYAxes = BuildYAxes();

            // Load each series sequentially to avoid List concurrency corruption
            var newSeries = new List<ISeries>();
            foreach (var s in SelectedSeries)
            {
                var line = await LoadSeriesAsync(s, from, to, newYAxes);
                if (line != null) newSeries.Add(line);
            }

            // Force onto UI thread — LiveCharts must receive property changes on the dispatcher
            await System.Windows.Application.Current.Dispatcher.InvokeAsync(() =>
            {
                XAxes = newXAxes;
                YAxes = newYAxes;
                Series = newSeries.ToArray();
            });

            System.Diagnostics.Debug.WriteLine($"[Trend] Series count: {Series.Length}");
            System.Diagnostics.Debug.WriteLine($"[Trend] YAxes count: {YAxes.Length}");
            foreach (var s in Series)
                System.Diagnostics.Debug.WriteLine($"[Trend] Series: {s.Name}, values: {(s.Values as System.Collections.ICollection)?.Count ?? -1}");

            UpdateStatus();
        }
        catch (Exception ex)
        {
            Status = $"Load failed: {ex.Message}";
        }
        finally
        {
            IsLoading = false;
        }
    }

    private Axis[] BuildYAxes()
    {
        var axes = new List<Axis>();
        var unitsSeen = new List<string>();

        foreach (var s in SelectedSeries)
        {
            if (!unitsSeen.Contains(s.Unit))
            {
                unitsSeen.Add(s.Unit);
                // Bug fix: parentheses around (unitsSeen.Count - 1) before % operator
                var color = Palette[(unitsSeen.Count - 1) % Palette.Length];
                axes.Add(new Axis
                {
                    Name = s.Unit,
                    NamePaint = new SolidColorPaint(color),
                    LabelsPaint = new SolidColorPaint(color),
                    SeparatorsPaint = new SolidColorPaint(SKColor.Parse("#2A3A55")),
                    Position = axes.Count % 2 == 0
                        ? LiveChartsCore.Measure.AxisPosition.Start
                        : LiveChartsCore.Measure.AxisPosition.End,
                });
            }
        }

        if (axes.Count == 0)
        {
            axes.Add(new Axis
            {
                LabelsPaint = new SolidColorPaint(SKColor.Parse("#A9B7CF")),
                SeparatorsPaint = new SolidColorPaint(SKColor.Parse("#2A3A55")),
            });
        }

        return axes.ToArray();
    }

    private async Task<ISeries?> LoadSeriesAsync(
        SelectedSeriesVm seriesVm, DateTime from, DateTime to, Axis[] yAxes)
    {
        try
        {
            var data = await _trendData.GetHistoryAsync(
                seriesVm.PointConfigId, from, to, maxPoints: 2000, CancellationToken.None);

            var points = data
                .Select(d => new DateTimePoint(d.Timestamp, (double)d.Value))
                .ToList();

            int axisIndex = 0;
            for (int i = 0; i < yAxes.Length; i++)
            {
                if (yAxes[i].Name == seriesVm.Unit) { axisIndex = i; break; }
            }

            seriesVm.PointCount = points.Count;

            return new LineSeries<DateTimePoint>
            {
                Name = seriesVm.SeriesName,
                Values = new ObservableCollection<DateTimePoint>(points),
                ScalesYAt = axisIndex,
                Stroke = new SolidColorPaint(seriesVm.Color) { StrokeThickness = 1.5f },
                Fill = new SolidColorPaint(seriesVm.Color.WithAlpha(20)),
                GeometrySize = 0,
                LineSmoothness = 0,
            };
        }
        catch (Exception ex)
        {
            Status = $"Error loading {seriesVm.Label}: {ex.Message}";
            return null;
        }
    }

    // ── Time window ───────────────────────────────────────────────────────

    private (DateTime from, DateTime to) GetTimeWindow()
    {
        if (IsCustomRange) return (CustomFrom, CustomTo);

        var to = DateTime.Now;
        var from = SelectedRange switch
        {
            TimeRangeOption.Last15Min => to.AddMinutes(-15),
            TimeRangeOption.Last1Hour => to.AddHours(-1),
            TimeRangeOption.Last4Hours => to.AddHours(-4),
            TimeRangeOption.Last8Hours => to.AddHours(-8),
            TimeRangeOption.Last24Hours => to.AddHours(-24),
            TimeRangeOption.Last7Days => to.AddDays(-7),
            _ => to.AddHours(-1)
        };
        return (from, to);
    }

    private void UpdateStatus()
    {
        if (SelectedSeries.Count == 0) { Status = "Select points to trend."; return; }
        var total = SelectedSeries.Sum(s => s.PointCount);
        Status = $"{SelectedSeries.Count} series · {total:N0} points";
    }

    public void Dispose() { }
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

    private int _pointCount;
    public int PointCount
    {
        get => _pointCount;
        set => SetProperty(ref _pointCount, value);
    }

    public string ColorHex => $"#{Color.Red:X2}{Color.Green:X2}{Color.Blue:X2}";

    public SelectedSeriesVm(TrendablePointDto dto, SKColor color)
    {
        PointConfigId = dto.PointConfigId;
        Label = dto.Label;
        Unit = dto.Unit;
        SeriesName = $"{dto.DeviceName} — {dto.Label}";
        Color = color;
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
