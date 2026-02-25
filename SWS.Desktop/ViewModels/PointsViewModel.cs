using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SWS.Core.Models;
using SWS.Desktop.Services;
using System.Collections.ObjectModel;

namespace SWS.Desktop.ViewModels;

public partial class PointsViewModel : ObservableObject
{
    private readonly ConfigDataService _data;

    public ObservableCollection<DeviceConfig> Devices { get; } = new();
    public ObservableCollection<PointConfig> Points { get; } = new();

    public IReadOnlyList<ModbusPointArea> Areas { get; } = Enum.GetValues<ModbusPointArea>();
    public IReadOnlyList<PointDataType> DataTypes { get; } = Enum.GetValues<PointDataType>();

    [ObservableProperty] private DeviceConfig? _selectedDevice;
    [ObservableProperty] private PointConfig? _selectedPoint;

    // editor fields
    [ObservableProperty] private string _editKey = "";
    [ObservableProperty] private string _editLabel = "";
    [ObservableProperty] private string _editUnit = "";
    [ObservableProperty] private ModbusPointArea _editArea = ModbusPointArea.HoldingRegister;
    [ObservableProperty] private int _editAddress = 0;
    [ObservableProperty] private ushort _editLength = 1;
    [ObservableProperty] private PointDataType _editDataType = PointDataType.UInt16;
    [ObservableProperty] private decimal _editScale = 1m;
    [ObservableProperty] private int _editPollRateMs = 5000;
    [ObservableProperty] private bool _editEssential = false;
    [ObservableProperty] private bool _editLogToHistory = false;
    [ObservableProperty] private int _editHistoryIntervalMs = 60000;

    [ObservableProperty] private string _status = "";

    public PointsViewModel(ConfigDataService data)
    {
        _data = data;
        _ = LoadDevicesAsync();
    }

    partial void OnSelectedDeviceChanged(DeviceConfig? value)
    {
        if (value == null) return;
        _ = RefreshAsync();
        New();
    }

    partial void OnSelectedPointChanged(PointConfig? value)
    {
        if (value == null) return;

        EditKey = value.Key;
        EditLabel = value.Label;
        EditUnit = value.Unit;
        EditArea = value.Area;
        EditAddress = value.Address;
        EditLength = value.Length;
        EditDataType = value.DataType;
        EditScale = value.Scale;
        EditPollRateMs = value.PollRateMs;
        EditEssential = value.IsEssential;
        EditLogToHistory = value.LogToHistory;
        EditHistoryIntervalMs = value.HistoryIntervalMs;
    }

    private async Task LoadDevicesAsync()
    {
        var list = await _data.GetDevicesAsync(CancellationToken.None);
        Devices.Clear();
        foreach (var d in list) Devices.Add(d);
        SelectedDevice = Devices.FirstOrDefault();
    }

    [RelayCommand]
    private async Task RefreshAsync()
    {
        if (SelectedDevice == null) return;

        var list = await _data.GetPointsForDeviceAsync(SelectedDevice.Id, CancellationToken.None);
        Points.Clear();
        foreach (var p in list) Points.Add(p);

        Status = $"Loaded {Points.Count} points for {SelectedDevice.Name}.";
    }

    [RelayCommand]
    private void New()
    {
        SelectedPoint = null;
        EditKey = "";
        EditLabel = "";
        EditUnit = "";
        EditArea = ModbusPointArea.HoldingRegister;
        EditAddress = 0;          // <=0 means not configured -> poller skips
        EditLength = 1;
        EditDataType = PointDataType.UInt16;
        EditScale = 1m;
        EditPollRateMs = 5000;
        EditEssential = false;
        EditLogToHistory = false;
        EditHistoryIntervalMs = 60000;
        Status = "New point.";
    }

    partial void OnEditAreaChanged(ModbusPointArea value) => ApplyDataTypeGuards();
    partial void OnEditDataTypeChanged(PointDataType value) => ApplyDataTypeGuards();

    // ✅ call this whenever Area/DataType changes OR just before Save
    private void ApplyDataTypeGuards()
    {
        // If it’s a bit-area, force Bool (coils + discrete inputs are bits)
        if (EditArea is ModbusPointArea.Coil or ModbusPointArea.DiscreteInput)
            EditDataType = PointDataType.Bool;

        // If Bool, force stable config
        if (EditDataType == PointDataType.Bool)
        {
            EditLength = 1;
            EditScale = 1m;
            EditUnit = string.Empty;
            return;
        }

        // If 32-bit types, force 2 registers
        if (EditDataType is PointDataType.UInt32 or PointDataType.Int32 or PointDataType.Float32)
            EditLength = 2;
        else
            EditLength = 1;
    }


    [RelayCommand]
    private async Task SaveAsync()
    {
        if (SelectedDevice == null) { Status = "Select device."; return; }
        if (string.IsNullOrWhiteSpace(EditKey)) { Status = "Key required."; return; }

        if (SelectedPoint == null)
        {
            var p = new PointConfig
            {
                DeviceConfigId = SelectedDevice.Id,
                Key = EditKey.Trim(),
                Label = EditLabel.Trim(),
                Unit = EditUnit.Trim(),
                Area = EditArea,
                Address = EditAddress,
                Length = EditLength,
                DataType = EditDataType,
                Scale = EditScale,
                PollRateMs = EditPollRateMs,
                IsEssential = EditEssential,
                LogToHistory = EditLogToHistory,
                HistoryIntervalMs = EditHistoryIntervalMs
            };

            await _data.AddPointAsync(p, CancellationToken.None);
            Status = "Added point.";
        }
        else
        {
            SelectedPoint.Key = EditKey.Trim();
            SelectedPoint.Label = EditLabel.Trim();
            SelectedPoint.Unit = EditUnit.Trim();
            SelectedPoint.Area = EditArea;
            SelectedPoint.Address = EditAddress;
            SelectedPoint.Length = EditLength;
            SelectedPoint.DataType = EditDataType;
            SelectedPoint.Scale = EditScale;
            SelectedPoint.PollRateMs = EditPollRateMs;
            SelectedPoint.IsEssential = EditEssential;
            SelectedPoint.LogToHistory = EditLogToHistory;
            SelectedPoint.HistoryIntervalMs = EditHistoryIntervalMs;

            await _data.UpdatePointAsync(SelectedPoint, CancellationToken.None);
            Status = "Saved point.";
        }

        await RefreshAsync();
    }

    [RelayCommand]
    private async Task DeleteAsync()
    {
        if (SelectedPoint == null) { Status = "Select point."; return; }

        await _data.DeletePointAsync(SelectedPoint.Id, CancellationToken.None);
        Status = "Deleted point.";
        await RefreshAsync();
        New();
    }
}