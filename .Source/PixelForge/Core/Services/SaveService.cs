using System.IO;
using PixelForge.Core.Model;
using PixelForge.Core.Providers.Images;

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

        internal string ResolveOutputFolder(IImageSource? source)
        {
            if (source is MemoryImageSource)
            {
                return CustomFolderPath;
            }

            if (source is ResourceImageSource file)
            {
                return Mode switch
                {
                    SaveMode.NextToOriginal => Path.Combine(Path.GetDirectoryName(file.Path)!, "compressed"),
                    SaveMode.CustomFolder => CustomFolderPath,
                    SaveMode.OverwriteOriginal => Path.GetDirectoryName(file.Path)!,
                    _ => CustomFolderPath
                };
            }

            return CustomFolderPath;
        }

        internal string SaveResult(IImageSource? source, OptimizationResult result)
        {
            string folder = ResolveOutputFolder(source);
            Directory.CreateDirectory(folder);

            string? baseName = Path.GetFileNameWithoutExtension(source?.Name);
            string? fileName = $"{baseName}{result.Extension}";
            string? fullPath = Path.Combine(folder, fileName);

            if (Mode == SaveMode.OverwriteOriginal && source is ResourceImageSource file)
            {
                string sourceExt = Path.GetExtension(file.Path).ToLowerInvariant();
                fullPath = sourceExt == result.Extension ? file.Path : Path.Combine(Path.GetDirectoryName(file.Path)!, fileName);
            }
            else
            {
                fullPath = ResolveUniqueFilePath(fullPath);
            }

            File.WriteAllBytes(fullPath, result.Bytes);
            return fullPath;
        }

        private static string ResolveUniqueFilePath(string path)
        {
            if (File.Exists(path))
            {
                string dir = Path.GetDirectoryName(path)!;
                string name = Path.GetFileNameWithoutExtension(path);
                string ext = Path.GetExtension(path);

                int i = 1;
                string candidate;
                do { candidate = Path.Combine(dir, $"{name} ({i++}){ext}"); }
                while (File.Exists(candidate));

                return candidate;
            }

            return path;
        }
    }
}