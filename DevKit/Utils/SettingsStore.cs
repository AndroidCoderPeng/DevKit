using System;
using System.IO;
using Newtonsoft.Json;

namespace DevKit.Utils
{
    // TODO NDK存储会把其他的配置清空
    public class SettingsStore
    {
        private static readonly string Dir = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "DevKit"
        );

        private static string GetFilePath(string fileName) => Path.Combine(Dir, fileName);

        public static T Load<T>(string fileName) where T : class, new()
        {
            var file = GetFilePath(fileName);
            Console.WriteLine($@"[SettingsStore] 加载配置：{file}");

            try
            {
                if (!File.Exists(file)) return new T();
                return JsonConvert.DeserializeObject<T>(File.ReadAllText(file)) ?? new T();
            }
            catch (Exception e)
            {
                Console.WriteLine($@"[SettingsStore] 加载失败：{e.Message}");
                return new T(); // 文件损坏时回退默认值，应用照常启动
            }
        }

        public static void Save<T>(string fileName, T settings)
        {
            var file = GetFilePath(fileName);
            Console.WriteLine($@"[SettingsStore] 保存配置：{file}");

            try
            {
                Directory.CreateDirectory(Dir);
                File.WriteAllText(file, JsonConvert.SerializeObject(settings, Formatting.Indented));
            }
            catch (Exception e)
            {
                Console.WriteLine($@"[SettingsStore] 保存失败：{e.Message}");
            }
        }
    }
}