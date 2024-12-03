using System;
using System.Diagnostics;
using System.Drawing;
using System.Globalization;
using System.Windows.Data;

namespace DevKit.Converters
{
    public class ColorTagForegroundConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            Debug.Assert(value != null, nameof(value) + " != null");
            var color = ColorTranslator.FromHtml((string)value);
            //L=0.2126×R+0.7152×G+0.0722×B

            var red = color.R / 255.0f;
            var green = color.G / 255.0f;
            var blue = color.B / 255.0f;

            // 非线性RGB值
            var r = red <= 0.03928f ? red / 12.92f : (float)Math.Pow((red + 0.055f) / 1.055f, 2.4f);
            var g = green <= 0.03928f ? green / 12.92f : (float)Math.Pow((green + 0.055f) / 1.055f, 2.4f);
            var b = blue <= 0.03928f ? blue / 12.92f : (float)Math.Pow((blue + 0.055f) / 1.055f, 2.4f);

            var luminance = 0.2126f * r + 0.7152f * g + 0.0722f * b;
            //如果相对亮度大于0.5，则认为是浅色
            return luminance > 0.5f ? "Black" : "White";
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return null;
        }
    }
}