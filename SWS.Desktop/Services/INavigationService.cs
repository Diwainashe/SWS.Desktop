namespace SWS.Desktop.Services;

public interface INavigationService
{
    object? CurrentView { get; }
    Task NavigateToAsync(AppPageKey key);
    Task NavigateToDeviceAsync(int deviceId, string deviceName, SWS.Core.Models.DeviceType deviceType);
}

public enum AppPageKey
{
    Dashboard,
    Devices,
    Points,
    Trend,
    Settings,
    DeviceDetail
}