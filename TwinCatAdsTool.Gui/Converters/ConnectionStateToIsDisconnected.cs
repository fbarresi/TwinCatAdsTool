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
    public class ConnectionStateToIsDisconnected : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is ConnectionState)
            {
                var connectionState = (ConnectionState) value;
                switch (connectionState)
                {
                    case ConnectionState.None:
                    case ConnectionState.Lost:
                        return true;
                    case ConnectionState.Disconnected:
                        return true;
                    case ConnectionState.Connected:
                        return false;
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
