using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SWS.Core.Models;
using SWS.Core.Profiles;
using SWS.Desktop.Services;
using System.Collections.ObjectModel;

namespace SWS.Desktop.ViewModels;

public partial class DevicesViewModel : ObservableObject
{
    private readonly ConfigDataService _data;
    private readonly DeviceProfileRegistry _profiles;

    public ObservableCollection<DeviceConfig> Devices { get; } = new();

    public ObservableCollection<DeviceType> DeviceTypes { get; }
        = new(Enum.GetValues<DeviceType>());

    public bool CanLoadTemplate =>
    SelectedDevice != null && EditDeviceType != DeviceType.Generic;

    [ObservableProperty] private DeviceConfig? _selectedDevice;

    [ObservableProperty] private string _editName = "";
    [ObservableProperty] private string _editIp = "";
    [ObservableProperty] private int _editPort = 502;
    [ObservableProperty] private byte _editUnitId = 1;
    [ObservableProperty] private int _editPollMs = 1000;
    [ObservableProperty] private bool _editEnabled = true;
    [ObservableProperty] private DeviceType _editDeviceType = DeviceType.Generic;

    [ObservableProperty] private string _status = "";

    public DevicesViewModel(ConfigDataService data,
                            DeviceProfileRegistry profiles)
    {
        _data = data;
        _profiles = profiles;
        _ = RefreshAsync();
    }

    partial void OnSelectedDeviceChanged(DeviceConfig? value)
    {
        if (value == null) return;

        EditName = value.Name;
        EditIp = value.IpAddress;
        EditPort = value.Port;
        EditUnitId = value.UnitId;
        EditPollMs = value.PollMs;
        EditEnabled = value.IsEnabled;
        EditDeviceType = value.DeviceType;
        OnPropertyChanged(nameof(CanLoadTemplate));
    }

    [RelayCommand]
    private async Task RefreshAsync()
    {
        var list = await _data.GetDevicesAsync(CancellationToken.None);
        Devices.Clear();
        foreach (var d in list) Devices.Add(d);
        Status = $"Loaded {Devices.Count} devices.";
    }

    [RelayCommand]
    private void New()
    {
        SelectedDevice = null;
        EditName = "";
        EditIp = "";
        EditPort = 502;
        EditUnitId = 1;
        EditPollMs = 1000;
        EditEnabled = true;
        EditDeviceType = DeviceType.Generic;
        Status = "New device.";
    }

    [RelayCommand]
    private async Task SaveAsync()
    {
        if (string.IsNullOrWhiteSpace(EditName) ||
            string.IsNullOrWhiteSpace(EditIp))
        {
            Status = "Name + IP required.";
            return;
        }

        if (SelectedDevice == null)
        {
            var d = new DeviceConfig
            {
                Name = EditName.Trim(),
                IpAddress = EditIp.Trim(),
                Port = EditPort,
                UnitId = EditUnitId,
                PollMs = EditPollMs,
                IsEnabled = EditEnabled,
                DeviceType = EditDeviceType
            };

            await _data.AddDeviceAsync(d, CancellationToken.None);
            Status = "Added.";
        }
        else
        {
            SelectedDevice.Name = EditName.Trim();
            SelectedDevice.IpAddress = EditIp.Trim();
            SelectedDevice.Port = EditPort;
            SelectedDevice.UnitId = EditUnitId;
            SelectedDevice.PollMs = EditPollMs;
            SelectedDevice.IsEnabled = EditEnabled;
            SelectedDevice.DeviceType = EditDeviceType;

            await _data.UpdateDeviceAsync(SelectedDevice, CancellationToken.None);
            Status = "Saved.";
        }

        await RefreshAsync();
    }

    [RelayCommand]
    private async Task DeleteAsync()
    {
        if (SelectedDevice == null)
        {
            Status = "Select a device.";
            return;
        }

        await _data.DeleteDeviceAsync(SelectedDevice.Id,
                                      CancellationToken.None);
        Status = "Deleted.";
        await RefreshAsync();
        New();
    }

    // =========================
    // DB-Template Loader
    // =========================
    [RelayCommand]
    private async Task LoadTemplateAsync()
    {
        if (SelectedDevice == null)
        {
            Status = "Select a device first.";
            return;
        }

        if (EditDeviceType == DeviceType.Generic)
        {
            Status = "Select a specific device type first (not Generic).";
            return;
        }

        // Ensure device type is actually saved to DB before we load its templates
        SelectedDevice.DeviceType = EditDeviceType;
        await _data.UpdateDeviceAsync(SelectedDevice, CancellationToken.None);

        // Copy templates -> PointConfigs (skips duplicate Keys)
        int added = await _data.AddDefaultPointsFromTemplatesAsync(
            SelectedDevice.Id,
            EditDeviceType,
            CancellationToken.None);

        Status = added == 0
            ? $"No templates found for {EditDeviceType} (or already loaded)."
            : $"Loaded {added} template points for {EditDeviceType}.";

        OnPropertyChanged(nameof(CanLoadTemplate));
    }
}