using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SWS.Desktop.Services;

namespace SWS.Desktop.ViewModels;

public partial class DashboardViewModel : ObservableObject
{
    public INavigationService Navigation { get; }

    public DashboardViewModel(INavigationService navigation)
    {
        Navigation = navigation;

        // default page
        _ = Navigation.NavigateToAsync(AppPageKey.Dashboard);
    }

    [RelayCommand]
    private Task GoDashboardAsync() => Navigation.NavigateToAsync(AppPageKey.Dashboard);

    [RelayCommand]
    private Task GoDevicesAsync() => Navigation.NavigateToAsync(AppPageKey.Devices);

    [RelayCommand]
    private Task GoPointsAsync() => Navigation.NavigateToAsync(AppPageKey.Points);
}