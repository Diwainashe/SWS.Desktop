using SWS.Desktop.ViewModels;
using System.ComponentModel;
using System.Windows.Controls;
using System.Windows.Data;

namespace SWS.Desktop.Views;

public partial class TrendView : UserControl
{
    public TrendView(TrendViewModel vm)
    {
        InitializeComponent();
        DataContext = vm;

        // Group available points by DeviceName in the picker
        var cvs = new CollectionViewSource { Source = vm.AvailablePoints };
        cvs.GroupDescriptions.Add(new PropertyGroupDescription(nameof(TrendablePointVm.DeviceName)));
        PointPickerList.ItemsSource = cvs.View;
    }
}
