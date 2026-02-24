using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.EntityFrameworkCore;
using SWS.Core.Models;
using SWS.Data;

namespace SWS.Desktop.ViewModels;

/// <summary>
/// Config screen VM:
/// - Manage devices
/// - Manage points per selected device
/// Uses IDbContextFactory to avoid DbContext concurrency issues.
/// </summary>
public partial class ConfigViewModel : ObservableObject
{
    private readonly IDbContextFactory<SwsDbContext> _dbFactory;

    public ObservableCollection<DeviceConfig> Devices { get; } = new();
    public ObservableCollection<PointConfig> Points { get; } = new();

    [ObservableProperty] private DeviceConfig? _selectedDevice;
    [ObservableProperty] private PointConfig? _selectedPoint;

    [ObservableProperty] private string _status = "Ready.";

    public ConfigViewModel(IDbContextFactory<SwsDbContext> dbFactory)
    {
        _dbFactory = dbFactory;
    }

    /// <summary>
    /// Call once when the view loads.
    /// </summary>
    public async Task InitializeAsync()
    {
        await RefreshDevicesAsync();
    }

    partial void OnSelectedDeviceChanged(DeviceConfig? value)
    {
        // Fire & forget is okay here because we isolate DbContext per call.
        _ = RefreshPointsAsync();
    }

    // ---------------- Devices ----------------

    [RelayCommand]
    public async Task RefreshDevicesAsync()
    {
        Status = "Loading devices...";
        Devices.Clear();
        Points.Clear();
        SelectedPoint = null;

        await using var db = await _dbFactory.CreateDbContextAsync();

        var list = await db.DeviceConfigs
            .AsNoTracking()
            .OrderBy(d => d.Name)
            .ToListAsync();

        foreach (var d in list)
            Devices.Add(d);

        SelectedDevice = Devices.FirstOrDefault();
        Status = $"Devices loaded: {Devices.Count}";
    }

    [RelayCommand]
    public void AddDevice()
    {
        var d = new DeviceConfig
        {
            Name = "New Device",
            IpAddress = "192.168.0.10",
            Port = 502,
            UnitId = 1,
            PollMs = 1000,
            IsEnabled = true
        };

        Devices.Add(d);
        SelectedDevice = d;
        Status = "New device added (not saved yet).";
    }

    [RelayCommand(CanExecute = nameof(CanSaveDevice))]
    public async Task SaveDeviceAsync()
    {
        if (SelectedDevice is null) return;

        Status = "Saving device...";
        await using var db = await _dbFactory.CreateDbContextAsync();

        // If Id==0, it's new. Otherwise update existing.
        if (SelectedDevice.Id == 0)
        {
            db.DeviceConfigs.Add(SelectedDevice);
        }
        else
        {
            db.DeviceConfigs.Update(SelectedDevice);
        }

        await db.SaveChangesAsync();

        Status = "Device saved.";
        await RefreshDevicesAsync();
    }

    private bool CanSaveDevice() => SelectedDevice is not null;

    [RelayCommand(CanExecute = nameof(CanDeleteDevice))]
    public async Task DeleteDeviceAsync()
    {
        if (SelectedDevice is null) return;

        Status = "Deleting device...";
        await using var db = await _dbFactory.CreateDbContextAsync();

        // Also delete points + readings for this device to keep DB clean (MVP).
        var deviceId = SelectedDevice.Id;

        // Load minimal tracked entities for delete
        var device = await db.DeviceConfigs.FirstOrDefaultAsync(d => d.Id == deviceId);
        if (device is null)
        {
            Status = "Device already deleted.";
            await RefreshDevicesAsync();
            return;
        }

        var points = await db.PointConfigs.Where(p => p.DeviceConfigId == deviceId).ToListAsync();
        db.PointConfigs.RemoveRange(points);

        var latest = await db.LatestReadings.Where(r => r.DeviceConfigId == deviceId).ToListAsync();
        db.LatestReadings.RemoveRange(latest);

        var history = await db.ReadingHistories.Where(h => h.DeviceConfigId == deviceId).ToListAsync();
        db.ReadingHistories.RemoveRange(history);

        db.DeviceConfigs.Remove(device);

        await db.SaveChangesAsync();
        Status = "Device deleted.";

        await RefreshDevicesAsync();
    }

    private bool CanDeleteDevice() => SelectedDevice is not null && SelectedDevice.Id != 0;

    // ---------------- Points ----------------

    [RelayCommand]
    public async Task RefreshPointsAsync()
    {
        Points.Clear();
        SelectedPoint = null;

        if (SelectedDevice is null || SelectedDevice.Id == 0)
        {
            Status = "Select a saved device to manage points.";
            return;
        }

        Status = "Loading points...";
        await using var db = await _dbFactory.CreateDbContextAsync();

        var list = await db.PointConfigs
            .AsNoTracking()
            .Where(p => p.DeviceConfigId == SelectedDevice.Id)
            .OrderBy(p => p.Key)
            .ToListAsync();

        foreach (var p in list)
            Points.Add(p);

        Status = $"Points loaded: {Points.Count}";
    }

    [RelayCommand(CanExecute = nameof(CanAddPoint))]
    public void AddPoint()
    {
        if (SelectedDevice is null || SelectedDevice.Id == 0) return;

        var p = new PointConfig
        {
            DeviceConfigId = SelectedDevice.Id,

            // You will fill these in
            Key = "Weight.Display",
            Label = "Weight",
            Unit = "kg",

            Area = ModbusPointArea.HoldingRegister,
            Address = 0,              // 0 => unconfigured, poller will skip
            Length = 1,
            DataType = PointDataType.Int16,
            Scale = 1m,
            PollRateMs = 500,
            IsEssential = true,

            LogToHistory = false,
            HistoryIntervalMs = 60000
        };

        Points.Add(p);
        SelectedPoint = p;
        Status = "New point added (not saved yet). Address=0 means 'not configured' (won't poll).";
    }

    private bool CanAddPoint() => SelectedDevice is not null && SelectedDevice.Id != 0;

    [RelayCommand(CanExecute = nameof(CanSavePoint))]
    public async Task SavePointAsync()
    {
        if (SelectedPoint is null) return;

        Status = "Saving point...";
        await using var db = await _dbFactory.CreateDbContextAsync();

        // Ensure the point belongs to selected device
        SelectedPoint.DeviceConfigId = SelectedDevice?.Id ?? SelectedPoint.DeviceConfigId;

        if (SelectedPoint.Id == 0)
            db.PointConfigs.Add(SelectedPoint);
        else
            db.PointConfigs.Update(SelectedPoint);

        await db.SaveChangesAsync();

        Status = "Point saved.";
        await RefreshPointsAsync();
    }

    private bool CanSavePoint() => SelectedPoint is not null && SelectedDevice is not null && SelectedDevice.Id != 0;

    [RelayCommand(CanExecute = nameof(CanDeletePoint))]
    public async Task DeletePointAsync()
    {
        if (SelectedPoint is null || SelectedPoint.Id == 0) return;

        Status = "Deleting point...";
        await using var db = await _dbFactory.CreateDbContextAsync();

        var pointId = SelectedPoint.Id;

        var point = await db.PointConfigs.FirstOrDefaultAsync(p => p.Id == pointId);
        if (point is null)
        {
            Status = "Point already deleted.";
            await RefreshPointsAsync();
            return;
        }

        // Clean readings for that point
        var latest = await db.LatestReadings.Where(r => r.PointConfigId == pointId).ToListAsync();
        db.LatestReadings.RemoveRange(latest);

        var hist = await db.ReadingHistories.Where(h => h.PointConfigId == pointId).ToListAsync();
        db.ReadingHistories.RemoveRange(hist);

        db.PointConfigs.Remove(point);
        await db.SaveChangesAsync();

        Status = "Point deleted.";
        await RefreshPointsAsync();
    }

    private bool CanDeletePoint() => SelectedPoint is not null && SelectedPoint.Id != 0;

    // Optional: called by DataGrid edit events to re-check CanExecute
    [RelayCommand]
    public void RequeryCommands()
    {
        SaveDeviceCommand.NotifyCanExecuteChanged();
        DeleteDeviceCommand.NotifyCanExecuteChanged();
        AddPointCommand.NotifyCanExecuteChanged();
        SavePointCommand.NotifyCanExecuteChanged();
        DeletePointCommand.NotifyCanExecuteChanged();
    }
}