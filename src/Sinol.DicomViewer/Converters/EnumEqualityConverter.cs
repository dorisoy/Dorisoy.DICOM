using System.Globalization;
using System.Windows.Data;
using Binding = System.Windows.Data.Binding;

namespace Sinol.DicomViewer.Converters;

public sealed class EnumEqualityConverter : IValueConverter
{
    public object? Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is null || parameter is null)
        {
            return false;
        }

        return value.Equals(parameter);
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is bool boolean && boolean)
        {
            return parameter;
        }

        return Binding.DoNothing;
    }
}
