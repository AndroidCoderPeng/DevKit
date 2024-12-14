using System;
using System.Diagnostics;
using System.Globalization;
using System.Windows.Data;

namespace DevKit.Converters
{
    public class AlphaValueConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            Debug.Assert(value != null, nameof(value) + " != null");
            var alpha = (int)((double)value / byte.MaxValue * 100);
            return $"{alpha}%";
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return null;
        }
    }
}