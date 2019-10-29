using System;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Windows.Data;
using TwinCAT.PlcOpen;

namespace TwinCatAdsTool.Gui.Converters
{
    public class DtToDateTimeConverter : IValueConverter
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
            if (value is DT dt)
            {
                return dt.Date;
            }

            if (value is DateTime dateTime)
            {
                return new DT(dateTime);
            }

            return value;
        }
    }
}