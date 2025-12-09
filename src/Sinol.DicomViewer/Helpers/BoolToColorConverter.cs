using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace Sinol.DicomViewer.Helpers;

/// <summary>
/// 布尔值转颜色转换器
/// Parameter 格式: "TrueColor|FalseColor" (例如: "#FFEF4444|#FF10B981")
/// </summary>
public class BoolToColorConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is bool boolValue && parameter is string colorPair)
        {
            var colors = colorPair.Split('|');
            if (colors.Length == 2)
            {
                var colorString = boolValue ? colors[0] : colors[1];
                return (System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString(colorString);
            }
        }

        return Colors.Transparent;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
