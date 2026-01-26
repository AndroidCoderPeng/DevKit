using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Drawing;
using System.Drawing.Imaging;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows.Media.Imaging;

namespace DevKit.Utils
{
    public static class MethodExtensions
    {
        /// <summary>
        /// 将adb形如【MemTotal:       11691976 kB】的返回值转为字典值
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        public static Dictionary<string, string> ToDictionary(this string value)
        {
            var strings = value.Split(new[] { "\r\n" }, StringSplitOptions.RemoveEmptyEntries);
            var dictionary = strings.Select(temp => temp.Split(new[] { ":" }, StringSplitOptions.None)
            ).ToDictionary(
                split => split[0].Trim(), split => split[1].Trim()
            );
            return dictionary;
        }

        /// <summary>
        /// 计算文件占用空间大小
        /// </summary>
        /// <param name="length"></param>
        /// <returns></returns>
        public static string FormatFileSize(this long length)
        {
            var size = (double)length / (1024 * 1024);
            return $"{size:F2}MB";
        }

        /// <summary>
        /// List 转 ObservableCollection
        /// </summary>
        /// <param name="list"></param>
        /// <returns></returns>
        public static ObservableCollection<T> ToObservableCollection<T>(this List<T> list)
        {
            var collection = new ObservableCollection<T>();
            foreach (var t in list)
            {
                collection.Add(t);
            }

            return collection;
        }

        /// <summary>
        /// 文件大小转换
        /// </summary>
        /// <param name="length"></param>
        /// <returns></returns>
        public static string ToFileSize(this long length)
        {
            if (length < 1024)
            {
                return $"{length}B";
            }

            if (length < 1024 * 1024)
            {
                return $"{(double)length / 1024:F2}KB";
            }

            if (length < 1024 * 1024 * 1024)
            {
                return $"{(double)length / (1024 * 1024):F2}MB";
            }

            return $"{(double)length / (1024 * 1024 * 1024):F2}GB";
        }

        /// <summary>
        /// 判断是否是Hex
        /// </summary>
        /// <returns></returns>
        public static bool IsHex(this string value)
        {
            if (value.Contains(" "))
            {
                value = value.Replace(" ", "");
            }

            return new Regex(@"^[0-9A-Fa-f]{2,}$").IsMatch(value);
        }

        public static bool IsByte(this string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return false;
            }

            return byte.TryParse(value, NumberStyles.None, CultureInfo.InvariantCulture, out _);
        }

        public static bool IsNumber(this string s)
        {
            return new Regex(@"^\d+$").IsMatch(s);
        }

        public static BitmapImage ToBitmapImage(this Bitmap bitmap)
        {
            var ms = new MemoryStream();
            bitmap.Save(ms, ImageFormat.Png);
            var bitImage = new BitmapImage();
            bitImage.BeginInit();
            bitImage.StreamSource = ms;
            bitImage.EndInit();
            return bitImage;
        }

        public static bool IsWebSocketUrl(this string url)
        {
            return url.StartsWith("ws") || url.StartsWith("wss");
        }
    }
}