using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SWS.Desktop.Services;
using SWS.Desktop.Views;

namespace SWS.Desktop.ViewModels;

/// <summary>
/// Shell VM: owns navigation only.
/// </summary>
public partial class MainShellViewModel : ObservableObject
{
    public INavigationService Navigation { get; }

    public MainShellViewModel(INavigationService navigation)
    {
        Navigation = navigation;
    }

    [RelayCommand]
    private void GoDashboard() => Navigation.NavigateTo<DashboardView>();

    [RelayCommand]
    private void GoConfig() => Navigation.NavigateTo<ConfigView>();
}