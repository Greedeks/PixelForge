using System.IO;
using ImageMagick;
using PixelForge.Core.Model;
using PixelForge.Core.Providers.Images;
using PixelForge.Helpers.Managers;

namespace PixelForge.Helpers.ImageOptimization
{
    internal static class ImageOptimizer
    {
        private const uint PngQuantizeColors = 256;
        private static readonly uint[] QualityLevels = [90, 85, 80, 75, 70];

        internal static OptimizationResult Optimize(IImageSource source)
        {
            string ext = source.Extension.ToLowerInvariant();

            long sizeBefore = source.SizeBytes ?? 0;

            return ext switch
            {
                ".png" => OptimizePng(source, sizeBefore),
                ".jpg" or ".jpeg" => OptimizeJpeg(source, sizeBefore),
                ".webp" => OptimizeWebp(source, sizeBefore),
                ".svg" => OptimizeSvg(source, sizeBefore),
                _ => throw new NotSupportedException()
            };
        }

        private static OptimizationResult OptimizePng(IImageSource source, long sizeBefore)
        {
            byte[]? bestBytes = null;

            try { bestBytes = EncodePngLossless(source); }
            catch (Exception ex) { MessageManager.Show(ex); }

            try
            {
                byte[] quantizedBytes = EncodePngQuantized(source);
                if (quantizedBytes != null)
                {
                    if (bestBytes == null || quantizedBytes.Length < bestBytes.Length)
                    {
                        bestBytes = quantizedBytes;
                    }
                }
            }
            catch (Exception ex) { MessageManager.Show(ex); }

            return ProcessResultOrFallback(bestBytes, source, ".png", sizeBefore);
        }

        private static byte[] EncodePngLossless(IImageSource source)
        {
            using Stream stream = source.OpenRead();
            using MagickImage image = new(stream);
            image.Strip();
            image.Format = MagickFormat.Png;
            image.Quality = 100;
            return image.ToByteArray();
        }

        private static byte[] EncodePngQuantized(IImageSource source)
        {
            using Stream stream = source.OpenRead();
            using MagickImage image = new(stream);
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


        private static OptimizationResult OptimizeJpeg(IImageSource source, long sizeBefore)
        {
            byte[]? bestBytes = null;
            object lockObj = new();

            Parallel.ForEach(QualityLevels, quality =>
            {
                try
                {
                    byte[] bytes = EncodeJpeg(source, quality);
                    using MemoryStream ms = new(bytes);
                    new ImageMagick.ImageOptimizer { OptimalCompression = true }.LosslessCompress(ms);
                    ms.Position = 0;
                    byte[] optimized = ms.ToArray();

                    lock (lockObj)
                    {
                        if (bestBytes == null || optimized.Length < bestBytes.Length)
                        {
                            bestBytes = optimized;
                        }
                    }
                }
                catch (Exception ex) { MessageManager.Show(ex); }
            });

            return ProcessResultOrFallback(bestBytes, source, ".jpg", sizeBefore);
        }

        private static byte[] EncodeJpeg(IImageSource source, uint quality)
        {
            using Stream stream = source.OpenRead();
            using MagickImage image = new(stream);

            image.Strip();

            image.Format = MagickFormat.Jpeg;
            image.Quality = quality;
            image.Settings.Interlace = Interlace.Jpeg;
            image.Settings.SetDefine(MagickFormat.Jpeg, "optimize-coding", "true");
            image.Settings.SetDefine(MagickFormat.Jpeg, "sampling-factor", "2x2,1x1,1x1");

            return image.ToByteArray();
        }

        private static OptimizationResult OptimizeWebp(IImageSource source, long sizeBefore)
        {
            byte[]? bestBytes = null;
            object lockObj = new();

            Parallel.ForEach(QualityLevels, quality =>
            {
                try
                {
                    byte[] bytes = EncodeWebp(source, quality);

                    lock (lockObj)
                    {
                        if (bestBytes == null || bytes.Length < bestBytes.Length)
                        {
                            bestBytes = bytes;
                        }
                    }
                }
                catch (Exception ex) { MessageManager.Show(ex); }
            });

            return ProcessResultOrFallback(bestBytes, source, ".webp", sizeBefore);
        }

        private static byte[] EncodeWebp(IImageSource source, uint quality)
        {
            using Stream stream = source.OpenRead();
            using MagickImage image = new(stream);

            image.Strip();

            image.Format = MagickFormat.WebP;
            image.Quality = quality;
            image.Settings.SetDefine(MagickFormat.WebP, "method", "6");
            image.Settings.SetDefine(MagickFormat.WebP, "segments", "4");
            image.Settings.SetDefine(MagickFormat.WebP, "sns-strength", "80");
            image.Settings.SetDefine(MagickFormat.WebP, "filter-strength", "35");

            return image.ToByteArray();
        }

        private static OptimizationResult OptimizeSvg(IImageSource source, long sizeBefore)
        {
            try
            {
                using Stream stream = source.OpenRead();
                using StreamReader reader = new(stream);
                string xml = reader.ReadToEnd();
                string minified = SvgOptimizer.Optimize(xml);
                byte[] bytes = System.Text.Encoding.UTF8.GetBytes(minified);

                return ProcessResultOrFallback(bytes, source, ".svg", sizeBefore);
            }
            catch { return ProcessResultOrFallback(null, source, ".svg", sizeBefore); }
        }

        private static OptimizationResult ProcessResultOrFallback(byte[]? optimizedBytes, IImageSource source, string extension, long sizeBefore)
        {
            byte[]? finalBytes = optimizedBytes;

            if (finalBytes == null || finalBytes.LongLength >= sizeBefore)
            {
                using Stream stream = source.OpenRead();

                using MemoryStream ms = new();
                stream.CopyTo(ms);

                finalBytes = ms.ToArray();
            }

            return new OptimizationResult
            {
                Bytes = finalBytes,
                Extension = extension,
                SizeBeforeBytes = sizeBefore,
                SizeAfterBytes = finalBytes.LongLength
            };
        }
    }
}
