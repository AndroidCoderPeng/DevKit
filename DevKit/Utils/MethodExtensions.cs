using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Text.RegularExpressions;

namespace DevKit.Utils
{
    public static class MethodExtensions
    {
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
        
        public static bool IsNumber(this string s)
        {
            return new Regex(@"^\d+$").IsMatch(s);
        }

        public static bool IsWebSocketUrl(this string url)
        {
            return url.StartsWith("ws") || url.StartsWith("wss");
        }
    }
}