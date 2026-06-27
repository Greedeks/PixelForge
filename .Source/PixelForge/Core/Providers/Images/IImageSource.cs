using System.IO;

namespace PixelForge.Core.Providers.Images
{
    internal interface IImageSource
    {
        string Name { get; }
        string Extension { get; }
        long? SizeBytes { get; }
        bool IsInMemory { get; }
        Stream OpenRead();
    }
}
