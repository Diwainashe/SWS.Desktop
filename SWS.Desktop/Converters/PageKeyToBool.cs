using System;
using System.Globalization;
using System.Windows.Data;
using SWS.Desktop.Services;

namespace SWS.Desktop.Converters;

/// <summary>
/// Converts CurrentPage (AppPageKey) to bool for RadioButton IsChecked.
/// Also supports ConvertBack so clicking a tab sets CurrentPage.
/// </summary>
public sealed class PageKeyToBoolConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is not AppPageKey current) return false;
        if (parameter is not AppPageKey target) return false;
        return current == target;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        // Only act when the RadioButton becomes checked
        if (value is bool b && b && parameter is AppPageKey target)
            return target;

        return Binding.DoNothing;
    }
}