using System.Windows;
using SWS.Desktop.ViewModels;

namespace SWS.Desktop;

public partial class MainWindow : Window
{
    public MainWindow(MainShellViewModel vm)
    {
        InitializeComponent();
        DataContext = vm;
    }
}