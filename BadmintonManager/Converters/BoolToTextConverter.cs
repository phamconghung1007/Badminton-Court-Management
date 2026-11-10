using System;
using System.Globalization;
using System.Windows.Data;

namespace BadmintonManager.Converters;

public class BoolToTextConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        bool b = value is bool flag && flag;
        if (parameter is string paramStr)
        {
            var parts = paramStr.Contains(';') ? paramStr.Split(';') : paramStr.Split('|');
            if (parts.Length >= 2)
            {
                return b ? parts[0].Trim() : parts[1].Trim();
            }
            if (parts.Length == 1)
            {
                return b ? parts[0].Trim() : string.Empty;
            }
        }

        return b ? "True" : "False";
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotSupportedException();
    }
}
