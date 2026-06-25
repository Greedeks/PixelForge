using System.Diagnostics;
using System.IO;
using ImageMagick;
using PixelForge.Core.Model;

namespace PixelForge.Helpers.ImageOptimization
{
    internal static class ImageOptimizer
    {
        private const int DefaultLossyQuality = 85;
        private const int PngQuantizeColors = 256;

        internal static OptimizationResult Optimize(string sourcePath)
        {
            string ext = Path.GetExtension(sourcePath).ToLowerInvariant();
            long sizeBefore = new FileInfo(sourcePath).Length;

            return ext switch
            {
                ".png" => OptimizePng(sourcePath, sizeBefore),
                ".jpg" or ".jpeg" => OptimizeJpeg(sourcePath, sizeBefore),
                ".webp" => OptimizeWebp(sourcePath, sizeBefore),
                ".svg" => OptimizeSvg(sourcePath, sizeBefore),
                _ => throw new NotSupportedException($"Формат '{ext}' не поддерживается оптимизатором.")
            };
        }

        private static OptimizationResult OptimizePng(string path, long sizeBefore)
        {
            byte[]? bestBytes = null;

            try { bestBytes = EncodePngLossless(path); }
            catch (Exception ex) { Debug.WriteLine($"Lossless failed: {ex.Message}"); }

            try
            {
                byte[] quantizedBytes = EncodePngQuantized(path);
                if (quantizedBytes != null)
                {
                    if (bestBytes == null || quantizedBytes.Length < bestBytes.Length)
                    {
                        bestBytes = quantizedBytes;
                    }
                }
            }
            catch (Exception ex) { Debug.WriteLine($"Quantized failed: {ex.Message}"); }

            return ProcessResultOrFallback(bestBytes, path, ".png", sizeBefore);
        }

        private static byte[] EncodePngLossless(string path)
        {
            using MagickImage image = new(path);
            image.Strip();
            image.Format = MagickFormat.Png;
            image.Quality = 100;
            return image.ToByteArray();
        }

        private static byte[] EncodePngQuantized(string path)
        {
            using MagickImage image = new(path);
            image.Strip();

            QuantizeSettings settings = new()
            {
                Colors = PngQuantizeColors,
                DitherMethod = DitherMethod.No
            };
            image.Quantize(settings);

            image.Settings.SetDefine(MagickFormat.Png, "color-type", "3");
            image.Format = MagickFormat.Png;
            image.Quality = 95;

            using MemoryStream ms = new();
            image.Write(ms);
            ms.Position = 0;

            ImageMagick.ImageOptimizer optimizer = new() { OptimalCompression = true };
            optimizer.Compress(ms);

            return ms.ToArray();
        }

        private static OptimizationResult OptimizeJpeg(string path, long sizeBefore)
        {
            try
            {
                using MagickImage image = new(path);
                image.Strip();
                image.Format = MagickFormat.Jpg;
                image.Quality = DefaultLossyQuality;

                return ProcessResultOrFallback(image.ToByteArray(), path, ".jpg", sizeBefore);
            }
            catch { return ProcessResultOrFallback(null, path, ".jpg", sizeBefore); }
        }

        private static OptimizationResult OptimizeWebp(string path, long sizeBefore)
        {
            try
            {
                using MagickImage image = new(path);
                image.Strip();
                image.Format = MagickFormat.WebP;
                image.Quality = DefaultLossyQuality;

                return ProcessResultOrFallback(image.ToByteArray(), path, ".webp", sizeBefore);
            }
            catch { return ProcessResultOrFallback(null, path, ".webp", sizeBefore); }
        }

        private static OptimizationResult OptimizeSvg(string path, long sizeBefore)
        {
            try
            {
                string xml = File.ReadAllText(path);
                string minified = SvgOptimizer.Optimize(xml);
                byte[] bytes = System.Text.Encoding.UTF8.GetBytes(minified);

                return ProcessResultOrFallback(bytes, path, ".svg", sizeBefore);
            }
            catch { return ProcessResultOrFallback(null, path, ".svg", sizeBefore); }
        }

        private static OptimizationResult ProcessResultOrFallback(byte[]? optimizedBytes, string originalPath, string extension, long sizeBefore, Exception? exception = null)
        {
            if (exception != null)
            {
                Debug.WriteLine($"Optimizer Error [{extension}]: {exception}");
            }

            byte[]? finalBytes = optimizedBytes;

            if (finalBytes == null || finalBytes.LongLength >= sizeBefore)
            {
                finalBytes = File.ReadAllBytes(originalPath);
            }

            return new OptimizationResult { Bytes = finalBytes, Extension = extension, SizeBeforeBytes = sizeBefore, SizeAfterBytes = finalBytes.LongLength };
        }
    }
}
