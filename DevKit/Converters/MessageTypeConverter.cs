using System;
using System.Globalization;
using System.Windows.Data;

namespace DevKit.Converters
{
    public class MessageTypeConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is int isSend)
            {
                return isSend == 1 ? "发送" : "接收";
            }

            return "未知";
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return null;
        }
    }
}