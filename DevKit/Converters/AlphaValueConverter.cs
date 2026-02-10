using System;
using System.Globalization;
using System.Windows.Data;

namespace DevKit.Converters
{
    public class AlphaValueConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value == null) return "100%";

            // 如果是字符串，先尝试解析
            if (value is string s)
            {
                if (string.IsNullOrWhiteSpace(s)) return "0%";
                if (!double.TryParse(s, NumberStyles.Any, culture, out var parsed)) return "0%";
                var alpha = (int)(parsed / byte.MaxValue * 100);
                return $"{alpha}%";
            }

            // 处理任何数值类型（byte/int/double 等）
            try
            {
                var d = System.Convert.ToDouble(value, culture);
                var alpha = (int)(d / byte.MaxValue * 100);
                return $"{alpha}%";
            }
            catch
            {
                return "0%";
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return null;
        }
    }
}