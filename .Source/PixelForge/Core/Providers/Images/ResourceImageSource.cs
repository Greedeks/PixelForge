using System.IO;

namespace PixelForge.Core.Providers.Images
{
    internal sealed class ResourceImageSource : IImageSource
    {
        internal string Path { get; }

        public string Name => System.IO.Path.GetFileName(Path);
        public string Extension => System.IO.Path.GetExtension(Path);
        public long? SizeBytes => new FileInfo(Path).Length;
        public bool IsInMemory => false;

        internal ResourceImageSource(string path)
        {
            Path = path;
        }

        public Stream OpenRead() => File.OpenRead(Path);
    }
}
