using System;
using System.Globalization;
using System.Windows.Data;

namespace DevKit.Converters
{
    public class MessageTagBgConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is int isSend)
            {
                return isSend == 1 ? "LimeGreen" : "OrangeRed";
            }

            return "LightGray";
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return null;
        }
    }
}