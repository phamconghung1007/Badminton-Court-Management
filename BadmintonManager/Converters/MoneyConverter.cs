using System;
using System.Globalization;
using System.Windows.Data;

namespace BadmintonManager.Converters;

public class MoneyConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is decimal d)
        {
            return $"{d:N0} đ";
        }
        if (value is double db)
        {
            return $"{db:N0} đ";
        }
        if (value is int i)
        {
            return $"{i:N0} đ";
        }
        return "0 đ";
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
