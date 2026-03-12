using System.ComponentModel;
using System.Windows.Controls;
using SWS.Desktop.ViewModels;

namespace SWS.Desktop.Views;

public partial class TrendView : UserControl
{
    private readonly TrendViewModel _vm;

    public TrendView(TrendViewModel vm)
    {
        InitializeComponent();
        _vm = vm;
        DataContext = vm;
        _vm.PropertyChanged += OnVmPropertyChanged;
    }

    private void OnVmPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        // LiveCharts2 WPF has a known bug where the SkiaSharp canvas doesn't
        // self-invalidate when bindings update after initial layout. Calling
        // Update() directly is the official workaround.
        if (e.PropertyName == nameof(TrendViewModel.Series))
        {
            Dispatcher.InvokeAsync(() => MainChart.CoreChart.Update(),
                System.Windows.Threading.DispatcherPriority.Render);
        }
    }
}
