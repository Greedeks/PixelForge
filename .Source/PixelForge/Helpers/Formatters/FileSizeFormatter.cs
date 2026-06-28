namespace PixelForge.Helpers.Formatters
{
    internal static class FileSizeFormatter
    {
        public static string Format(long bytes) => bytes switch
        {
            < 1_024 => $"{bytes} B",
            < 1_024 * 1_024 => $"{bytes / 1_024.0:0.#} KB",
            < 1_024L * 1_024 * 1_024 => $"{bytes / (1_024.0 * 1_024):0.##} MB",
            _ => $"{bytes / (1_024.0 * 1_024 * 1_024):0.##} GB"
        };
    }
}
