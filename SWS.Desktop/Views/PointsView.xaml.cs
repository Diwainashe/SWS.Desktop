using System.Windows.Controls;
using SWS.Desktop.ViewModels;

namespace SWS.Desktop.Views;

public partial class PointsView : UserControl
{
    public PointsView(PointsViewModel vm)
    {
        InitializeComponent();
        DataContext = vm;
    }
}