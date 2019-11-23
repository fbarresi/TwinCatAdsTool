using System;
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Data;
using TwinCAT;

namespace TwinCatAdsTool.Gui.Converters
{
    public class ConnectionStateToIconConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is ConnectionState)
            {
                var connectionState = (ConnectionState)value;
                switch (connectionState)
                {
                    case ConnectionState.None:
                    case ConnectionState.Lost:
                        return "minus";
                    case ConnectionState.Disconnected:
                        return "unlink";
                    case ConnectionState.Connected:
                        return "link";
                    default:
                        return DependencyProperty.UnsetValue;

                }
            }

            return DependencyProperty.UnsetValue;

        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return DependencyProperty.UnsetValue;
        }
    }
}