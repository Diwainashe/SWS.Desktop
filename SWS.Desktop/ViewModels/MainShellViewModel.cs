using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SWS.Desktop.Services;

namespace SWS.Desktop.ViewModels;

/// <summary>
/// Shell VM: owns navigation only.
/// MainWindow binds its top-bar buttons to these commands.
/// </summary>
public partial class MainShellViewModel : ObservableObject
{
    public INavigationService Navigation { get; }

    public MainShellViewModel(INavigationService navigation)
    {
        Navigation = navigation;

        // Optional: ensure the app lands on Dashboard when it starts
        _ = Navigation.NavigateToAsync(AppPageKey.Dashboard);
    }

    [RelayCommand]
    private async Task GoDashboardAsync()
        => await Navigation.NavigateToAsync(AppPageKey.Dashboard);

    [RelayCommand]
    private async Task GoDevicesAsync()
        => await Navigation.NavigateToAsync(AppPageKey.Devices);

    [RelayCommand]
    private async Task GoPointsAsync()
        => await Navigation.NavigateToAsync(AppPageKey.Points);
}