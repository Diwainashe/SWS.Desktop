using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.Extensions.DependencyInjection;
using SWS.Core.Models;
using SWS.Desktop.ViewModels;
using SWS.Desktop.Views;

namespace SWS.Desktop.Services;

/// <summary>
/// Navigation service that keeps the entire app inside ONE window.
/// It creates a fresh DI scope per page to avoid:
/// - DbContext cross-thread issues
/// - scoped service injected into singleton VM issues
/// - stale view models / stale data contexts
/// </summary>
public sealed partial class AppNavigationService : ObservableObject, INavigationService
{
    private readonly IServiceProvider _root;

    // We keep the active page scope so all its scoped services live together,
    // and we dispose them when navigating away.
    private IServiceScope? _currentScope;

    [ObservableProperty]
    private object? _currentView;

    private readonly Dictionary<AppPageKey, Type> _routes = new()
    {
        { AppPageKey.Dashboard,    typeof(DashboardView) },
        { AppPageKey.Devices,      typeof(DevicesView) },
        { AppPageKey.Points,       typeof(PointsView) },
        { AppPageKey.Settings,     typeof(SettingsView) },
        { AppPageKey.DeviceDetail, typeof(DeviceDetailView) },
    };

    public AppNavigationService(IServiceProvider root) => _root = root;

    public Task NavigateToAsync(AppPageKey key)
    {
        if (!_routes.TryGetValue(key, out var viewType))
            throw new InvalidOperationException($"No route for {key}");

        // Dispose the previous page scope (releases DbContexts, services, etc.)
        _currentScope?.Dispose();
        _currentScope = _root.CreateScope();

        // Resolve the view *inside the scope* so it can safely use scoped services
        CurrentView = _currentScope.ServiceProvider.GetRequiredService(viewType);

        return Task.CompletedTask;
    }

    /// <summary>
    /// Navigate to device detail, seeding the VM with the device context.
    /// </summary>
    public Task NavigateToDeviceAsync(int deviceId, string deviceName, DeviceType deviceType)
    {
        _currentScope?.Dispose();
        _currentScope = _root.CreateScope();

        var view = _currentScope.ServiceProvider.GetRequiredService<DeviceDetailView>();

        // Seed the VM — it's already resolved and wired as DataContext by the view ctor
        if (view.DataContext is DeviceDetailViewModel vm)
            vm.SetDevice(deviceId, deviceName, deviceType);

        CurrentView = view;
        return Task.CompletedTask;
    }
}
