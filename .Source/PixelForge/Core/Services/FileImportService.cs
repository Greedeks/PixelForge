using System.IO;
using Ookii.Dialogs.Wpf;
using PixelForge.Core.Providers.Images;

namespace PixelForge.Core.Services
{
    internal static class FileImportService
    {
        internal static string[] ImportFiles(string filter, bool multiselect = true)
        {
            VistaOpenFileDialog dialog = new()
            {
                Multiselect = multiselect,
                Filter = filter
            };

            return dialog.ShowDialog() == true ? dialog.FileNames : [];
        }

        internal static IEnumerable<string> ExpandToSupportedFiles(IEnumerable<string> paths, bool recursive, params string[] supportedExtensions)
        {
            SearchOption searchOption = recursive ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly;

            HashSet<string> extensionsSet = new(supportedExtensions, StringComparer.OrdinalIgnoreCase);

            foreach (string path in paths)
            {
                if (Directory.Exists(path))
                {
                    foreach (string file in Directory.GetFiles(path, "*", searchOption))
                    {
                        if (IsSupported(file, extensionsSet))
                        {
                            yield return file;
                        }
                    }
                }
                else if (File.Exists(path) && IsSupported(path, extensionsSet))
                {
                    yield return path;
                }
            }
        }

        private static bool IsSupported(string path, HashSet<string> supportedExtensions) => supportedExtensions.Contains(Path.GetExtension(path));

        internal static IImageSource CreateSource(string path) => IsTemporaryPath(path) ? new MemoryImageSource(Path.GetFileName(path), File.ReadAllBytes(path)) : new ResourceImageSource(path);

        internal static bool IsTemporaryPath(string path) => path.StartsWith(Path.GetTempPath().TrimEnd(Path.DirectorySeparatorChar), StringComparison.OrdinalIgnoreCase);
    }
}
