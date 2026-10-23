using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;
using BadmintonManager.Models.Enums;

namespace BadmintonManager.Converters;

public class StatusToBrushConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is BookingStatus status)
        {
            return status switch
            {
                BookingStatus.Pending => new SolidColorBrush(Color.FromRgb(245, 158, 11)),      // Vàng hổ phách (#F59E0B)
                BookingStatus.Confirmed => new SolidColorBrush(Color.FromRgb(37, 99, 235)),    // Xanh dương (#2563EB)
                BookingStatus.CheckedIn => new SolidColorBrush(Color.FromRgb(16, 185, 129)),   // Xanh lá (#10B981)
                BookingStatus.Completed => new SolidColorBrush(Color.FromRgb(107, 114, 128)),  // Xám (#6B7280)
                BookingStatus.Cancelled => new SolidColorBrush(Color.FromRgb(239, 68, 68)),    // Đỏ (#EF4444)
                BookingStatus.NoShow => new SolidColorBrush(Color.FromRgb(234, 88, 12)),       // Cam đậm (#EA580C)
                _ => new SolidColorBrush(Colors.LightGray)
            };
        }
        return new SolidColorBrush(Colors.LightGray);
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
