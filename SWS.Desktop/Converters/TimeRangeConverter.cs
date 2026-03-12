using SWS.Desktop.ViewModels;
using System.Globalization;
using System.Windows.Data;

namespace SWS.Desktop.Converters;

public sealed class TimeRangeConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        => value is TimeRangeOption opt ? opt switch
        {
            TimeRangeOption.Last15Min => "Last 15 min",
            TimeRangeOption.Last1Hour => "Last 1 hour",
            TimeRangeOption.Last4Hours => "Last 4 hours",
            TimeRangeOption.Last8Hours => "Last 8 hours",
            TimeRangeOption.Last24Hours => "Last 24 hours",
            TimeRangeOption.Last7Days => "Last 7 days",
            TimeRangeOption.Custom => "Custom range",
            _ => opt.ToString()
        } : value?.ToString() ?? "";

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => throw new NotImplementedException();
}
