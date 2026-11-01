using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace BadmintonManager.Converters;

public class BoolToVisibilityConverter : IValueConverter
{
    public bool Invert { get; set; }

    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        bool isInverse = Invert || (parameter is string paramStr && paramStr.Equals("Inverse", StringComparison.OrdinalIgnoreCase));
        if (value is bool b)
        {
            if (isInverse) b = !b;
            return b ? Visibility.Visible : Visibility.Collapsed;
        }
        return Visibility.Visible;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        bool isInverse = Invert || (parameter is string paramStr && paramStr.Equals("Inverse", StringComparison.OrdinalIgnoreCase));
        if (value is Visibility v)
        {
            bool res = v == Visibility.Visible;
            return isInverse ? !res : res;
        }
        return false;
    }
}
