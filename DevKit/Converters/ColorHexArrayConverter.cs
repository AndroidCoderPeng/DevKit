using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;
using Color = System.Windows.Media.Color;
using Point = System.Windows.Point;

namespace DevKit.Converters
{
    public class ColorHexArrayConverter : IValueConverter
    {
        private readonly double[] _fiveValues = { 0.0, 0.25, 0.5, 0.75, 1.0 };
        private readonly double[] _fourValues = { 0.0, 0.33, 0.66, 1.0 };
        private readonly double[] _threeValues = { 0.0, 0.5, 1.0 };
        private readonly double[] _twoValues = { 0.0, 1.0 };

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            Debug.Assert(value != null, nameof(value) + " != null");
            var hexArray = (List<string>)value;
            var linearGradientBrush = new LinearGradientBrush
            {
                StartPoint = new Point(0, 0),
                EndPoint = new Point(1, 1)
            };

            double[] values = { };
            switch (hexArray.Count)
            {
                case 2:
                    values = _twoValues;
                    break;
                case 3:
                    values = _threeValues;
                    break;
                case 4:
                    values = _fourValues;
                    break;
                case 5:
                    values = _fiveValues;
                    break;
            }

            for (var i = 0; i < hexArray.Count; i++)
            {
                var colorHex = hexArray[i];
                var color = ColorTranslator.FromHtml(colorHex);
                var gradientStop = new GradientStop(Color.FromRgb(color.R, color.G, color.B), values[i]);
                linearGradientBrush.GradientStops.Add(gradientStop);
            }

            return linearGradientBrush;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return null;
        }
    }
}