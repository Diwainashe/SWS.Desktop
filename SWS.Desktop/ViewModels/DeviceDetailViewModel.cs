using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SWS.Core.Models;
using SWS.Core.Profiles;
using SWS.Desktop.Services;
using System.Collections.ObjectModel;
using System.Windows;

namespace SWS.Desktop.ViewModels;

// ── Section / row primitives ───────────────────────────────────────────────

/// <summary>A label+value pair used inside detail sections.</summary>
public sealed class DetailRowVm
{
    public string Label { get; init; } = "";
    public string Value { get; set; } = "—";
    /// <summary>Optional colour override for the value text (e.g. red for alarms).</summary>
    public string? ValueColor { get; set; }
}

/// <summary>A titled group of DetailRowVm items.</summary>
public sealed class DetailSectionVm
{
    public string Title { get; init; } = "";
    public ObservableCollection<DetailRowVm> Rows { get; } = new();
}

/// <summary>Compact ON/OFF tile used in I/O grid sections.</summary>
public sealed class IoTileVm
{
    public string Label { get; init; } = "";
    public bool IsOn { get; set; }
}

/// <summary>A titled group of IoTileVm items (inputs or outputs).</summary>
public sealed class IoSectionVm
{
    public string Title { get; init; } = "";
    public ObservableCollection<IoTileVm> Tiles { get; } = new();
}

// ── Main ViewModel ─────────────────────────────────────────────────────────

/// <summary>
/// Detail view for a single device.
/// Layout (top to bottom):
///   1. Header bar  – device name, run-state badge, refresh, back
///   2. Summary cards – Weight, Flowrate, Alarm status, Recipe
///   3. Live / Process section   – state register decoded rows
///   4. Active Recipe section    – 40201-40228 parameters
///   5. Accumulation section     – system / recipe / user ACUMs
///   6. I/O grid                 – IN1-12 + OUT1-16 compact tiles
///   7. Device Info              – version, compile date, edit date
///   8. All Points table         – full raw fallback
/// </summary>
public partial class DeviceDetailViewModel : ObservableObject, IDisposable
{
    private readonly ConfigDataService _data;
    private readonly DeviceProfileRegistry _profiles;
    private readonly LatestReadingsBus _bus;
    private readonly TemplateDiagnosticsService _templateDiag;

    private int _deviceId;
    private DeviceType _deviceType;

    // ── Summary cards ──────────────────────────────────────────────────────
    [ObservableProperty] private string _deviceName = "Device";
    [ObservableProperty] private string _weightDisplay = "—";
    [ObservableProperty] private string _flowrateDisplay = "—";
    [ObservableProperty] private string _runStateDisplay = "—";
    [ObservableProperty] private string _runStateColor = "#A9B7CF";
    [ObservableProperty] private string _recipeDisplay = "—";
    [ObservableProperty] private string _status = "Loading…";
    [ObservableProperty] private bool _hasAlarms = false;

    // ── Diagnostics ────────────────────────────────────────────────────────
    public DiagnosticsVm Diagnostics { get; } = new();

    // ── Organised sections ─────────────────────────────────────────────────
    public DetailSectionVm ProcessSection { get; } = new() { Title = "Process" };
    public DetailSectionVm RecipeSection { get; } = new() { Title = "Active Recipe" };
    public DetailSectionVm AccumSection { get; } = new() { Title = "Accumulation" };
    public DetailSectionVm DeviceInfoSection { get; } = new() { Title = "Device Info" };
    public IoSectionVm InputsSection { get; } = new() { Title = "Digital Inputs" };
    public IoSectionVm OutputsSection { get; } = new() { Title = "Digital Outputs" };

    // ── Full point list (raw fallback) ─────────────────────────────────────
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

    public void SetDevice(int deviceId, string deviceName, DeviceType deviceType)
    {
        _deviceId = deviceId;
        _deviceType = deviceType;
        DeviceName = deviceName;
        _ = RefreshAsync();
    }

    private void OnLatestUpdated(object? sender, LatestReadingsUpdatedEventArgs e)
    {
        Application.Current.Dispatcher.InvokeAsync(async () =>
        {
            try { await RefreshAsync(); }
            catch (Exception ex) { Status = $"Refresh error: {ex.Message}"; }
        });
    }

    [RelayCommand]
    private async Task RefreshAsync()
    {
        var all = await _data.GetLatestReadingsAsync(CancellationToken.None);
        var readings = all.Where(r => r.DeviceId == _deviceId).ToList();

        if (readings.Count == 0) { Status = "No readings yet."; return; }

        var profile = _profiles.Get(_deviceType);

        // ── Summary cards ──────────────────────────────────────────────────
        UpdateSummaryCards(readings, profile);

        // ── Diagnostics ────────────────────────────────────────────────────
        var diag = _templateDiag.Build(_deviceType, readings);
        Diagnostics.SetAlarmGroups(diag.AlarmGroups);
        Diagnostics.SetStateGroups(diag.StateGroups);
        HasAlarms = diag.AlarmGroups.Any(g =>
            g.Items.Any(i => !string.Equals(i, "OK", StringComparison.OrdinalIgnoreCase)));

        // ── Organised sections ─────────────────────────────────────────────
        BuildProcessSection(readings, profile);
        BuildRecipeSection(readings, profile);
        BuildAccumSection(readings, profile);
        BuildDeviceInfoSection(readings);
        BuildIoSections(readings);

        // ── Full point list ────────────────────────────────────────────────
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

    // ─────────────────────────────────────────────────────────────────────
    // Section builders
    // ─────────────────────────────────────────────────────────────────────

    private void UpdateSummaryCards(List<LatestReadingSnapshot> r, IDeviceProfile profile)
    {
        WeightDisplay = profile.FormatDisplay("Weight.Display", Val(r, "Weight.Display"), r);
        FlowrateDisplay = profile.FormatDisplay("Flowrate.Actual", Val(r, "Flowrate.Actual"), r);

        var opWord = ToU16(Val(r, "State.Operating") ?? 0);
        bool running = (opWord & 0x0001) != 0;
        bool hasAlarmBits = (opWord & 0x3000) != 0; // bits 12+13: OVER/UNDER

        (RunStateDisplay, RunStateColor) = hasAlarmBits ? ("ALARM", "#FF4444")
                                         : running ? ("Running", "#2ECC71")
                                                        : ("Stopped", "#A9B7CF");

        var recipeId = Val(r, "RecipeId_40201");
        RecipeDisplay = recipeId.HasValue ? $"#{(int)recipeId.Value}" : "—";
    }

    private void BuildProcessSection(List<LatestReadingSnapshot> r, IDeviceProfile profile)
    {
        RebuildSection(ProcessSection, new[]
        {
            Row(r, "Weight.Display",  profile, "Weight"),
            Row(r, "Flowrate.Actual", profile, "Flowrate"),
            Row(r, "State.Weight",    profile, "Weight State"),
            Row(r, "State.Operating", profile, "Operating State"),
            Row(r, "State.Condition", profile, "Condition State"),
        });
    }

    private void BuildRecipeSection(List<LatestReadingSnapshot> r, IDeviceProfile profile)
    {
        // Current recipe parameters: 40201-40228
        // Keys as stored by the template (address-suffixed names)
        var rows = new List<DetailRowVm?>
        {
            SimpleRow("Recipe ID",         Val(r, "RecipeId_40201"),        v => $"#{(int)v}"),
            ScaledRow("Target",            Val(r, "Target_40202"),           r, profile),
            ScaledRow("Co-Feed Remains",   Val(r, "CoFeedingRemains_40204"), r, profile),
            ScaledRow("Free Fall",         Val(r, "FreeFall_40206"),         r, profile),
            ScaledRow("Near Zero",         Val(r, "NearZero_40208"),         r, profile),
            SimpleRow("Start Delay",       Val(r, "StartDelay_40210"),       v => $"{v} ms"),
            SimpleRow("Result Wait Timer", Val(r, "ResultWaitingTimer_40213"),v => $"{v} ms"),
            SimpleRow("Discharge Delay",   Val(r, "DischargeDelay_40214"),   v => $"{v} ms"),
            SimpleRow("Over/Under Alarm",  Val(r, "OverUnderAlarmOnoff_40215"), v => v == 1 ? "ON" : "OFF"),
            ScaledRow("OVER limit",        Val(r, "Over_40218"),             r, profile),
            ScaledRow("UNDER limit",       Val(r, "Under_40220"),            r, profile),
            SimpleRow("Motor Group ID",    Val(r, "MotorGroupId_40228"),     v => $"{(int)v}"),
        };

        RebuildSection(RecipeSection, rows);
    }

    private void BuildAccumSection(List<LatestReadingSnapshot> r, IDeviceProfile profile)
    {
        RebuildSection(AccumSection, new[]
        {
            ScaledRow("System ACUM",      Val(r, "Systemacum_40114"),       r, profile),
            ScaledRow("Stock-In ACUM",    Val(r, "StockInAcum_40102"),      r, profile),
            ScaledRow("In/Out ACUM",      Val(r, "InOutAcumWeight_40106"),  r, profile),
            SimpleRow("In/Out ACUM Pcs",  Val(r, "InOutAcumPcs_40104"),     v => $"{(int)v}"),
            SimpleRow("Total ACUM Pcs",   Val(r, "TotalAcumPcs_40108"),     v => $"{(int)v}"),
            ScaledRow("User 0 ACUM",      Val(r, "User0Acum_40701"),        r, profile),
            ScaledRow("User 1 ACUM",      Val(r, "User1Acum_40707"),        r, profile),
        });
    }

    private void BuildDeviceInfoSection(List<LatestReadingSnapshot> r)
    {
        RebuildSection(DeviceInfoSection, new[]
        {
            SimpleRow("Version",      Val(r, "Version_40013"),     v => FormatDate((int)v)),
            SimpleRow("Compile Date", Val(r, "CompileDate_40015"), v => FormatDate((int)v)),
            SimpleRow("Edit Date",    Val(r, "EditDate_40017"),    v => FormatDate((int)v)),
        });
    }

    private void BuildIoSections(List<LatestReadingSnapshot> readings)
    {
        // Digital inputs IN1-IN12  (coil addresses 102-113)
        var inputKeys = new[] {
            ("In1_102","IN1"),("In2_103","IN2"),("In3_104","IN3"),("In4_105","IN4"),
            ("In5_106","IN5"),("In6_107","IN6"),("In7_108","IN7"),("In8_109","IN8"),
            ("In9_110","IN9"),("In10_111","IN10"),("In11_112","IN11"),("In12_113","IN12")
        };
        RebuildIo(InputsSection, readings, inputKeys);

        // Digital outputs OUT1-OUT16 (coil addresses 114-129)
        var outputKeys = new[] {
            ("Out1_114","OUT1"),("Out2_115","OUT2"),("Out3_116","OUT3"),("Out4_117","OUT4"),
            ("Out5_118","OUT5"),("Out6_119","OUT6"),("Out7_120","OUT7"),("Out8_121","OUT8"),
            ("Out9_122","OUT9"),("Out10_123","OUT10"),("Out11_124","OUT11"),("Out12_125","OUT12"),
            ("Out13_126","OUT13"),("Out14_127","OUT14"),("Out15_128","OUT15"),("Out16_129","OUT16")
        };
        RebuildIo(OutputsSection, readings, outputKeys);
    }

    // ─────────────────────────────────────────────────────────────────────
    // Helpers
    // ─────────────────────────────────────────────────────────────────────

    private static void RebuildSection(DetailSectionVm section, IEnumerable<DetailRowVm?> rows)
    {
        section.Rows.Clear();
        foreach (var row in rows)
            if (row != null) section.Rows.Add(row);
    }

    private static void RebuildIo(IoSectionVm section, List<LatestReadingSnapshot> readings,
        (string key, string label)[] definitions)
    {
        section.Tiles.Clear();
        foreach (var (key, label) in definitions)
        {
            var snap = readings.FirstOrDefault(r => r.Key == key);
            section.Tiles.Add(new IoTileVm
            {
                Label = label,
                IsOn = snap?.ValueNumeric is decimal v && v != 0m
            });
        }
    }

    /// <summary>Build a row using the device profile's FormatDisplay.</summary>
    private static DetailRowVm Row(List<LatestReadingSnapshot> readings, string key,
        IDeviceProfile profile, string label)
    {
        var snap = readings.FirstOrDefault(r => r.Key == key);
        return new DetailRowVm
        {
            Label = label,
            Value = snap != null
                ? profile.FormatDisplay(key, snap.ValueNumeric, readings)
                : "—"
        };
    }

    /// <summary>
    /// Build a row for a weight-like Int32 register that needs decimal scaling.
    /// Uses Weight.Decimal + Weight.Unit from readings (same scale as display weight).
    /// </summary>
    private static DetailRowVm ScaledRow(string label, decimal? raw,
        List<LatestReadingSnapshot> readings, IDeviceProfile profile)
    {
        if (raw == null) return new DetailRowVm { Label = label, Value = "—" };

        // Reuse Weight formatting: decimals + unit
        int decimals = GetInt(readings, "Weight.Decimal");
        int unitCode = GetInt(readings, "Weight.Unit");
        string unit = unitCode switch { 0 => "g", 1 => "kg", 2 => "t", 3 => "lb", _ => "" };

        decimal scaled = raw.Value / (decimal)Math.Pow(10, Math.Clamp(decimals, 0, 6));
        string num = Math.Round(scaled, decimals).ToString($"F{decimals}");
        string val = string.IsNullOrWhiteSpace(unit) ? num : $"{num} {unit}";

        return new DetailRowVm { Label = label, Value = val };
    }

    private static DetailRowVm? SimpleRow(string label, decimal? raw, Func<decimal, string> fmt)
    {
        if (raw == null) return null;
        return new DetailRowVm { Label = label, Value = fmt(raw.Value) };
    }

    private static decimal? Val(List<LatestReadingSnapshot> r, string key)
        => r.FirstOrDefault(x => x.Key == key)?.ValueNumeric;

    private static int GetInt(List<LatestReadingSnapshot> r, string key)
        => (int)(Val(r, key) ?? 0m);

    private static ushort ToU16(decimal d)
    {
        if (d < 0) return 0;
        if (d > ushort.MaxValue) return ushort.MaxValue;
        return (ushort)d;
    }

    /// <summary>
    /// GM9907 stores version/dates as packed integers: YYYYMMDD or version code.
    /// Display raw if it doesn't parse as a plausible date.
    /// </summary>
    private static string FormatDate(int raw)
    {
        if (raw <= 0) return "—";
        // Try YYYYMMDD
        int y = raw / 10000, m = (raw / 100) % 100, d = raw % 100;
        if (y is >= 2000 and <= 2100 && m is >= 1 and <= 12 && d is >= 1 and <= 31)
            return $"{y}-{m:D2}-{d:D2}";
        return raw.ToString();
    }

    // ── Navigation ─────────────────────────────────────────────────────────
    public event EventHandler? BackRequested;

    [RelayCommand]
    private void GoBack() => BackRequested?.Invoke(this, EventArgs.Empty);

    public void Dispose() => _bus.LatestUpdated -= OnLatestUpdated;
}
