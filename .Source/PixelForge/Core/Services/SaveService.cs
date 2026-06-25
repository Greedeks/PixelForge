using System.IO;
using PixelForge.Core.Model;

namespace PixelForge.Core.Services
{
    internal enum SaveMode
    {
        NextToOriginal,
        CustomFolder,
        OverwriteOriginal
    }

    internal sealed class SaveService
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

        internal string SaveResult(string sourceFilePath, OptimizationResult result)
        {
            string folder = ResolveOutputFolder(sourceFilePath);
            Directory.CreateDirectory(folder);

            string baseName = Path.GetFileNameWithoutExtension(sourceFilePath);
            string fileName = $"{baseName}{result.Extension}";
            string fullPath = Path.Combine(folder, fileName);

            if (Mode == SaveMode.OverwriteOriginal)
            {
                string sourceExt = Path.GetExtension(sourceFilePath).ToLowerInvariant();
                if (sourceExt == result.Extension)
                {
                    fullPath = sourceFilePath;
                }
                else
                {
                    fullPath = Path.Combine(Path.GetDirectoryName(sourceFilePath)!, fileName);
                }
            }
            else
            {
                fullPath = EnsureUniquePath(fullPath);
            }

            File.WriteAllBytes(fullPath, result.Bytes);
            return fullPath;
        }

        private static string EnsureUniquePath(string path)
        {
            if (!File.Exists(path))
            {
                return path;
            }

            string dir = Path.GetDirectoryName(path)!;
            string name = Path.GetFileNameWithoutExtension(path);
            string ext = Path.GetExtension(path);

            int i = 1;
            string candidate;
            do
            {
                candidate = Path.Combine(dir, $"{name} ({i}){ext}");
                i++;
            } while (File.Exists(candidate));

            return candidate;
        }
    }
}