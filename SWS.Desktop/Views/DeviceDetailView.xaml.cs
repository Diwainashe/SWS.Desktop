using SWS.Desktop.Services;
using SWS.Desktop.ViewModels;
using System.Windows.Controls;

namespace SWS.Desktop.Views;

public partial class DeviceDetailView : UserControl
{
    private readonly INavigationService _nav;

    public DeviceDetailView(DeviceDetailViewModel vm, INavigationService nav)
    {
        InitializeComponent();
        _nav = nav;
        DataContext = vm;

        // Wire the back event: VM raises it, view handles the nav call
        vm.BackRequested += OnBackRequested;
        Unloaded += (_, _) => vm.BackRequested -= OnBackRequested;
    }

    private void OnBackRequested(object? sender, EventArgs e)
    {
        _ = _nav.NavigateToAsync(AppPageKey.Dashboard);
    }
}
