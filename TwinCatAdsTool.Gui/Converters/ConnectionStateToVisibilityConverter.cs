using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;
using TwinCAT;

namespace TwinCatAdsTool.Gui.Converters
{
    public class ConnectionStateToVisibilityConverter : IValueConverter
    {
        public Visibility IfDisconnected { get; set; } = Visibility.Visible;
        public Visibility IfConnected { get; set; } = Visibility.Collapsed;
        
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is ConnectionState)
            {
                var connectionState = (ConnectionState) value;
                switch (connectionState)
                {
                    case ConnectionState.None:
                    case ConnectionState.Lost:
                    case ConnectionState.Disconnected:
                        return IfDisconnected;
                    case ConnectionState.Connected:
                        return IfConnected;
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