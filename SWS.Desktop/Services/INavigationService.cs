namespace SWS.Desktop.Services;

public interface INavigationService
{
    object? CurrentView { get; }
    Task NavigateToAsync(AppPageKey key);
}

public enum AppPageKey
{
    Dashboard,
    Devices,
    Points,
    Settings
}