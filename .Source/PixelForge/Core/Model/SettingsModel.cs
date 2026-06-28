using PixelForge.Core.Services;

namespace PixelForge.Core.Model
{
    internal class SettingsModel
    {
        private static SettingsModel? _instance;
        public static SettingsModel Instance => _instance ??= new SettingsModel();
        private SettingsModel() { }

        public static SaveMode SelectedSaveMode
        {
            get => SaveService.Instance.Mode;
            set => SaveService.Instance.Mode = value;
        }

        public string CustomFolderPath
        {
            get => SaveService.Instance.CustomFolderPath;
            set => SaveService.Instance.CustomFolderPath = value;
        }

        public bool AutoOptimizeOnLoad
        {
            get => SettingsService.Instance.AutoOptimizeOnLoad;
            set => SettingsService.Instance.AutoOptimizeOnLoad = value;
        }
    }
}
