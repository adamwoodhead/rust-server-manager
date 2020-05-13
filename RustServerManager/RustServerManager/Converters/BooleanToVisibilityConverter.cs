using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace RustServerManager.Converters
{
    public class BooleanToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if ((bool)value)
            {
                return Visibility.Visible;
            }
            else
            {
                return Visibility.Collapsed;
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is Visibility)
            {
                if ((Visibility)value == Visibility.Visible)
                {
                    return true;
                }
                else
                {
                    return false;
                }
            }

            return false;
        }
    }
}
