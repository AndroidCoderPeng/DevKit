using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
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
    }
}