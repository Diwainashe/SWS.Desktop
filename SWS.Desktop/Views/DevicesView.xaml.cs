using System.Windows.Controls;
using SWS.Desktop.ViewModels;

namespace SWS.Desktop.Views;

public partial class DevicesView : UserControl
{
    public DevicesView(DevicesViewModel vm)
    {
        InitializeComponent();
        DataContext = vm;
    }
}