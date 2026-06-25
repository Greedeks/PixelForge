using System.IO;
using System.Windows.Media.Imaging;
using SkiaSharp;
using Svg.Skia;

namespace PixelForge.Core.Services
{
    internal static class ImageThumbnailService
    {
        private const int DefaultThumbnailSize = 96;

        internal static BitmapSource? GetThumbnail(string path, int decodePixelWidth = DefaultThumbnailSize)
        {
            try
            {
                string ext = Path.GetExtension(path).ToLowerInvariant();

                return ext switch
                {
                    ".svg" => LoadSvgThumbnail(path, decodePixelWidth),
                    ".webp" => LoadWebpThumbnail(path, decodePixelWidth),
                    _ => LoadThumbnail(path, decodePixelWidth)
                };
            }
            catch
            {
                return null;
            }
        }

        private static BitmapSource? LoadThumbnail(string path, int decodePixelWidth)
        {
            BitmapImage bmp = new();
            bmp.BeginInit();
            bmp.UriSource = new Uri(path);
            bmp.DecodePixelWidth = decodePixelWidth;
            bmp.CacheOption = BitmapCacheOption.OnLoad;
            bmp.EndInit();
            bmp.Freeze();
            return bmp;
        }

        private static BitmapSource? LoadWebpThumbnail(string path, int decodePixelWidth)
        {
            using SKBitmap? original = SKBitmap.Decode(path);
            if (original is null)
            {
                return null;
            }

            double scale = decodePixelWidth / (double)original.Width;
            int targetWidth = decodePixelWidth;
            int targetHeight = Math.Max(1, (int)Math.Round(original.Height * scale));

            SKSamplingOptions samplingOptions = new SKSamplingOptions(SKFilterMode.Linear, SKMipmapMode.Linear);
            using SKBitmap resized = original.Resize(new SKImageInfo(targetWidth, targetHeight), samplingOptions);
            SKBitmap bitmapToEncode = resized ?? original;

            return EncodeToBitmapSource(bitmapToEncode);
        }

        private static BitmapSource? LoadSvgThumbnail(string path, int decodePixelWidth)
        {
            using SKSvg svg = new();
            SKPicture? picture = svg.Load(path);
            if (picture is null)
            {
                return null;
            }

            SKRect bounds = picture.CullRect;
            if (bounds.Width <= 0 || bounds.Height <= 0)
            {
                return null;
            }

            float scale = decodePixelWidth / bounds.Width;
            int targetWidth = decodePixelWidth;
            int targetHeight = Math.Max(1, (int)Math.Round(bounds.Height * scale));

            SKImageInfo info = new(targetWidth, targetHeight, SKColorType.Rgba8888, SKAlphaType.Premul);
            using SKSurface surface = SKSurface.Create(info);
            SKCanvas canvas = surface.Canvas;
            canvas.Clear(SKColors.Transparent);
            canvas.Scale((float)scale);
            canvas.DrawPicture(picture);
            canvas.Flush();

            using var snapshot = surface.Snapshot();
            using var skBitmap = SKBitmap.FromImage(snapshot);
            return EncodeToBitmapSource(skBitmap);
        }

        private static BitmapSource? EncodeToBitmapSource(SKBitmap skBitmap)
        {
            using SKData data = skBitmap.Encode(SKEncodedImageFormat.Png, 100);
            using MemoryStream stream = new();
            data.SaveTo(stream);
            stream.Position = 0;

            BitmapImage bmp = new();
            bmp.BeginInit();
            bmp.CacheOption = BitmapCacheOption.OnLoad;
            bmp.StreamSource = stream;
            bmp.EndInit();
            bmp.Freeze();
            return bmp;
        }
    }
}