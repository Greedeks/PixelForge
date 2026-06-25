namespace PixelForge.Core.Model
{
    public class OptimizationResult
    {
        public required byte[] Bytes { get; init; }
        public required string Extension { get; init; }
        public long SizeBeforeBytes { get; init; }
        public long SizeAfterBytes { get; init; }
        public double SavedPercent => SizeBeforeBytes <= 0 ? 0 : Math.Max(0, 100.0 * (SizeBeforeBytes - SizeAfterBytes) / SizeBeforeBytes);
    }
}
