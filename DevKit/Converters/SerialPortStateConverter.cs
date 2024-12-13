using System;
using System.Globalization;
using System.Windows.Data;

namespace DevKit.Converters
{
    public class SerialPortStateConverter: IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return value == null || value.Equals("打开串口");
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return null;
        }
    }
}