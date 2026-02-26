using System.Windows.Controls;
using SWS.Desktop.ViewModels;

namespace SWS.Desktop.Views;

public partial class SettingsView : UserControl
{
    public SettingsView(SettingsViewModel vm)
    {
        InitializeComponent();
        DataContext = vm;
    }
}