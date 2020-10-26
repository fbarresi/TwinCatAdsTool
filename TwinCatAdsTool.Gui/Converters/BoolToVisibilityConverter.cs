using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace TwinCatAdsTool.Gui.Converters
{
    public class BoolToVisibilityConverter : IValueConverter
    {
        public Visibility IfTrue { get; set; } = Visibility.Visible;
        public Visibility IfFalse { get; set; } = Visibility.Collapsed;
        
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool)
            {
                return (bool) value ? IfTrue : IfFalse;
            }

            return DependencyProperty.UnsetValue;

        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return DependencyProperty.UnsetValue;
        }
    }
}