using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace PixelForge.Core.Services
{
    internal sealed class SettingsService
    {
        private class SettingsData
        {
            public SaveMode SaveMode { get; set; } = SaveMode.NextToOriginal;
            public string CustomFolderPath { get; set; } = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
            public bool AutoOptimizeOnLoad { get; set; } = true;
        }

        private static SettingsService? _instance;
        internal static SettingsService Instance => _instance ??= new SettingsService();
        internal bool AutoOptimizeOnLoad { get; set; } = true;

        private static readonly string SettingsFile = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "pixelforge.settings.json");

        private static readonly JsonSerializerOptions JsonOptions = new()
        {
            WriteIndented = true,
            Converters = { new JsonStringEnumConverter() },
            Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
        };

        internal static void Load()
        {
            try
            {
                if (!File.Exists(SettingsFile))
                {
                    return;
                }

                string json = File.ReadAllText(SettingsFile);
                SettingsData? data = JsonSerializer.Deserialize<SettingsData>(json, JsonOptions);

                if (data is null)
                {
                    return;
                }

                SaveService.Instance.Mode = data.SaveMode;
                SaveService.Instance.CustomFolderPath = data.CustomFolderPath;
                Instance.AutoOptimizeOnLoad = data.AutoOptimizeOnLoad;
            }
            catch { }
        }

        internal static void Save()
        {
            try
            {
                SettingsData data = new()
                {
                    SaveMode = SaveService.Instance.Mode,
                    CustomFolderPath = SaveService.Instance.CustomFolderPath,
                    AutoOptimizeOnLoad = Instance.AutoOptimizeOnLoad
                };

                File.WriteAllText(SettingsFile, JsonSerializer.Serialize(data, JsonOptions));
            }
            catch { }
        }
    }
}