using System;
using System.Diagnostics;
using System.Globalization;
using System.Windows.Data;

namespace DevKit.Converters
{
    public class MessageTypeConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            Debug.Assert(value != null, nameof(value) + " != null");
            if ((bool)value)
            {
                return "发送";
            }

            return "接收";
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return null;
        }
    }
}