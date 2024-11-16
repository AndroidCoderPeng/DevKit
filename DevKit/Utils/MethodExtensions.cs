using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

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
            var dictionary = strings.Select(
                temp => temp.Split(new[] { ":" }, StringSplitOptions.None)
            ).ToDictionary(
                split => split[0].Trim(), split => split[1].Trim()
            );
            return dictionary;
        }

        /// <summary>
        /// 格式化内存值
        /// </summary>
        /// <param name="memory"></param>
        /// <returns></returns>
        public static double FormatMemoryValue(this string memory)
        {
            var newLine = Regex.Replace(memory, @"\s", "*");

            var temp = double.Parse(newLine.Split(new[] { "*" }, StringSplitOptions.RemoveEmptyEntries)[0]);

            //转为GB
            return Math.Round(temp / 1024 / 1024, 2);
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
        /// 判断是否是Hex
        /// </summary>
        /// <returns></returns>
        public static bool IsHex(this string value)
        {
            if (value.Contains("-"))
            {
                value = value.Replace("-", "");
            }
            else if (value.Contains(" "))
            {
                value = value.Replace(" ", "");
            }
            return new Regex(@"^[0-9A-Fa-f]{2,}$").IsMatch(value);
        }

        /// <summary>
        /// 返回的Hex带有 - ，比如：AA-BB-CC-DD，需要自行去掉
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        public static string StringToHex(this string value)
        {
            var bytes = Encoding.UTF8.GetBytes(value);
            return BitConverter.ToString(bytes);
        }

        /// <summary>
        /// Hex串转为byte[]
        /// </summary>
        /// <param name="hex"></param>
        /// <returns></returns>
        public static byte[] HexToBytes(this string hex)
        {
            //有些带有“-”，有些带有“ ”，先整理Hex格式
            if (hex.Contains("-"))
            {
                hex = hex.Replace("-", "");
            }
            else if (hex.Contains(" "))
            {
                hex = hex.Replace(" ", "");
            }

            var bytes = new byte[hex.Length / 2];
            for (var i = 0; i < hex.Length; i += 2)
            {
                bytes[i / 2] = Convert.ToByte(hex.Substring(i, 2), 16);
            }

            return bytes;
        }

        /// <summary>
        /// byte[]转字符串，中英文都可以，不会乱码
        /// </summary>
        /// <param name="bytes"></param>
        /// <returns></returns>
        public static string ByteArrayToString(this byte[] bytes)
        {
            return Encoding.UTF8.GetString(bytes);
        }

        public static bool IsNumber(this string s)
        {
            return new Regex(@"^\d+$").IsMatch(s);
        }
    }
}