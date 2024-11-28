using System;
using System.Diagnostics;
using System.Globalization;
using System.Windows.Data;

namespace DevKit.Converters
{
    public class MessageTagConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            Debug.Assert(value != null, nameof(value) + " != null");
            if ((int)value == 0)
            {
                return "Hidden";
            }

            return "Visible";
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return null;
        }
    }
}