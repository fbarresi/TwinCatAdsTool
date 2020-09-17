using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Data;
using TwinCAT;

namespace TwinCatAdsTool.Gui.Converters
{
    public class ConnectionStateToBoolConverter : IValueConverter
    {
        public bool IfDisconnected { get; set; } = true;
        public bool IfConnected { get; set; } = false;
        
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
