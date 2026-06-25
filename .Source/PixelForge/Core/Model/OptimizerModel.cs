namespace PixelForge.Core.Model
{
    internal class OptimizerModel
    {
        public string FilePath { get; init; } = string.Empty;
        public string FileName { get; init; } = string.Empty;
        public long SizeBytes { get; init; }

        public OptimizationResult? OptimizationResult { get; set; }
    }
}
