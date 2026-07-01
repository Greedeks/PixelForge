namespace PixelForge.Helpers.Formatters
{
    internal static class FileSizeFormatter
    {
        internal static string Format(long bytes) => bytes switch
        {
            < 1_000 => $"{bytes} B",
            < 1_000 * 1_024 => $"{bytes / 1_024.0:0.#} KB",
            < 1_000 * 1_024 * 1_024 => $"{bytes / (1_024.0 * 1_024):0.##} MB",
            _ => $"{bytes / (1_024.0 * 1_024 * 1_024):0.##} GB"
        };
    }
}
