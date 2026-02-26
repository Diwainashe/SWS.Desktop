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

    [ObservableProperty]
    private AppPageKey _currentPage = AppPageKey.Dashboard;

    public MainShellViewModel(INavigationService navigation)
    {
        Navigation = navigation;

        // initial route
        _ = Navigation.NavigateToAsync(CurrentPage);
    }

    partial void OnCurrentPageChanged(AppPageKey value)
    {
        _ = Navigation.NavigateToAsync(value);
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
    [RelayCommand] 
    private void GoSettings() 
        => _ = Navigation.NavigateToAsync(AppPageKey.Settings);
}