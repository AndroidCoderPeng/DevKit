using System.Collections.Generic;
using System.Collections.ObjectModel;

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
    }
}