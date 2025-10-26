using System.Globalization;

namespace AccreditValidation.Components.Converters;

public class InverseBoolConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is bool boolValue)
        {
            // If IsRead is true, return light background color (already read)
            // If IsRead is false, return highlighted background color (unread)
            if (targetType == typeof(Color))
            {
                return boolValue ? Color.FromArgb("#F5F5F5") : Color.FromArgb("#E3F2FD");
            }
            return !boolValue;
        }
        
        if (targetType == typeof(Color))
        {
            return Color.FromArgb("#E3F2FD"); // Default unread color
        }
        
        return true;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is bool boolValue)
        {
            return !boolValue;
        }
        return false;
    }
}