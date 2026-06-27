using System.IO;

namespace PixelForge.Core.Providers.Images
{
    internal sealed class MemoryImageSource : IImageSource
    {
        private readonly byte[] _bytes;

        public string Name { get; }
        public string Extension => Path.GetExtension(Name);
        public long? SizeBytes => _bytes.LongLength;
        public bool IsInMemory => true;

        internal MemoryImageSource(string originalName, byte[] bytes)
        {
            Name = originalName;
            _bytes = bytes;
        }

        public Stream OpenRead() => new MemoryStream(_bytes, writable: false);
    }
}
