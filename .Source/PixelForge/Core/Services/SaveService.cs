using System.IO;

namespace PixelForge.Core.Services
{
    internal enum SaveMode
    {
        NextToOriginal,
        CustomFolder,
        OverwriteOriginal
    }

    internal class SaveService
    {
        private static SaveService? _instance;

        internal static SaveService Instance => _instance ??= new SaveService();

        internal SaveMode Mode { get; set; } = SaveMode.NextToOriginal;

        internal string CustomFolderPath { get; set; } = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);

        internal string ResolveOutputFolder(string sourceFilePath)
        {
            return Mode switch
            {
                SaveMode.NextToOriginal => Path.Combine(Path.GetDirectoryName(sourceFilePath)!, "compressed"),
                SaveMode.CustomFolder => CustomFolderPath,
                SaveMode.OverwriteOriginal => Path.GetDirectoryName(sourceFilePath)!,
                _ => CustomFolderPath
            };
        }
    }
}