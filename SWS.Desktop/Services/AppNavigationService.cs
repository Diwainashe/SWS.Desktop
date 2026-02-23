using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Controls;
using Microsoft.Extensions.DependencyInjection;
using SWS.Desktop.Views;

namespace SWS.Desktop.Services;

/// <summary>
/// App-level navigation between views inside the main window.
/// </summary>
public sealed class AppNavigationService : INavigationService
{
    private readonly IServiceProvider _sp;
    private UserControl _currentView;

    public event PropertyChangedEventHandler? PropertyChanged;

    public AppNavigationService(IServiceProvider sp)
    {
        _sp = sp;

        // Default page
        _currentView = _sp.GetRequiredService<DashboardView>();
    }

    public UserControl CurrentView
    {
        get => _currentView;
        private set
        {
            if (ReferenceEquals(_currentView, value)) return;
            _currentView = value;
            OnPropertyChanged();
        }
    }

    public void NavigateTo<TView>() where TView : UserControl
    {
        CurrentView = _sp.GetRequiredService<TView>();
    }

    private void OnPropertyChanged([CallerMemberName] string? name = null)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}