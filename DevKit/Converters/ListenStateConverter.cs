﻿using System;
using System.Globalization;
using System.Windows.Data;

namespace DevKit.Converters
{
    public class ListenStateConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return value == null || value.Equals("监听");
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return null;
        }
    }
}