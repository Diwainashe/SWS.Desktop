using System.Windows.Controls;
using SWS.Desktop.ViewModels;

namespace SWS.Desktop.Views;

public partial class DashboardView : UserControl
{
    public DashboardView(DashboardViewModel vm)
    {
        InitializeComponent();
        DataContext = vm;
    }
}