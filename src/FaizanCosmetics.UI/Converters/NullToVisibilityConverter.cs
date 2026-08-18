using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace FaizanCosmetics.UI.Converters;

/// <summary>Collapses an element when the bound value is null or an empty string (used for conditionally-shown validation messages).</summary>
public class NullToVisibilityConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object parameter, CultureInfo culture) =>
        string.IsNullOrEmpty(value as string) ? Visibility.Collapsed : Visibility.Visible;

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}
