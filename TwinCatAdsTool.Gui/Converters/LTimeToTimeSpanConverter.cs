using System;
using System.Globalization;
using System.Linq;
using System.Windows.Data;
using TwinCAT.PlcOpen;

namespace TwinCatAdsTool.Gui.Converters
{
    public class LTimeToTimeSpanConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return DoConvert(value);
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return DoConvert(value);
        }

        private object DoConvert(object value)
        {
            if (value is LTIME lTime)
            {
                return lTime.Value;
            }

            if (value is TimeSpan timeSpan)
            {
                return new LTIME(timeSpan);
            }

            return value;
        }
    }
}