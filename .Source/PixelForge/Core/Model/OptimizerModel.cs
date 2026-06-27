using PixelForge.Core.Providers.Images;

namespace PixelForge.Core.Model
{
    internal class OptimizerModel
    {
        public required IImageSource Source { get; init; }

        public required string FileName { get; init; }

        public long SizeBytes { get; init; }

        public OptimizationResult? OptimizationResult { get; set; }
    }
}
