using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace FaizanCosmetics.UI.Converters;

/// <summary>Like the framework's BooleanToVisibilityConverter, with an optional "Invert" ConverterParameter for the common negated case (e.g. hide a panel when IsBusy is true).</summary>
public class BooleanToVisibilityConverter2 : IValueConverter
{
    public object Convert(object? value, Type targetType, object parameter, CultureInfo culture)
    {
        var flag = value is bool b && b;
        if (string.Equals(parameter as string, "Invert", StringComparison.OrdinalIgnoreCase))
        {
            flag = !flag;
        }
        return flag ? Visibility.Visible : Visibility.Collapsed;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}
