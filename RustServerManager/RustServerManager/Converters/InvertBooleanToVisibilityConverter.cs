using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace RustServerManager.Converters
{
    public class InvertBooleanToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if ((bool)value)
            {
                return Visibility.Collapsed;
            }
            else
            {
                return Visibility.Visible;
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is Visibility)
            {
                if ((Visibility)value == Visibility.Collapsed)
                {
                    return true;
                }
                else
                {
                    return false;
                }
            }

            return true;
        }
    }
}
