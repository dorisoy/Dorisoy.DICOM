using System.Globalization;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media;
using Point = System.Windows.Point;

namespace Sinol.DicomViewer.Converters;

public sealed class MeasurementPointsConverter : IValueConverter
{
    public object? Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is Point[] points)
        {
            return new PointCollection(points);
        }

        return null;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => throw new NotSupportedException();
}
