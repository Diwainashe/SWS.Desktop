using System.ComponentModel;
using System.Windows.Controls;

namespace SWS.Desktop.Services;

/// <summary>
/// Simple navigation abstraction: swap the active screen (UserControl) inside the main window.
/// </summary>
public interface INavigationService : INotifyPropertyChanged
{
    UserControl CurrentView { get; }
    void NavigateTo<TView>() where TView : UserControl;
}