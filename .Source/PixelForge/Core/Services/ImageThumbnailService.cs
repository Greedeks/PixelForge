using System.IO;
using System.Text;
using System.Windows.Media.Imaging;
using PixelForge.Core.Providers.Images;
using SkiaSharp;
using Svg.Skia;

namespace PixelForge.Core.Services
{
    internal static class ImageThumbnailService
    {
        private const int DefaultThumbnailSize = 96;

        internal static BitmapSource? GetThumbnail(IImageSource source, int decodePixelWidth = DefaultThumbnailSize)
        {
            try
            {
                string ext = source.Extension.ToLowerInvariant();

                return ext switch
                {
                    ".svg" => RenderSvgThumbnail(source, decodePixelWidth),
                    ".webp" => RenderWebpThumbnail(source, decodePixelWidth),
                    _ => RenderThumbnail(source, decodePixelWidth)
                };
            }
            catch
            {
                return null;
            }
        }

        private static BitmapSource? RenderThumbnail(IImageSource source, int decodePixelWidth)
        {
            using Stream stream = source.OpenRead();

            BitmapImage bmp = new();
            bmp.BeginInit();
            bmp.StreamSource = stream;
            bmp.DecodePixelWidth = decodePixelWidth;
            bmp.CacheOption = BitmapCacheOption.OnLoad;
            bmp.EndInit();
            bmp.Freeze();

            return bmp;
        }

        private static BitmapSource? RenderWebpThumbnail(IImageSource source, int decodePixelWidth)
        {
            using Stream stream = source.OpenRead();
            using SKBitmap? original = SKBitmap.Decode(stream);

            if (original is null)
            {
                return null;
            }

            double scale = decodePixelWidth / (double)original.Width;
            int targetWidth = decodePixelWidth;
            int targetHeight = Math.Max(1, (int)Math.Round(original.Height * scale));

            SKSamplingOptions samplingOptions = new(SKFilterMode.Linear, SKMipmapMode.Linear);

            using SKBitmap resized = original.Resize(new SKImageInfo(targetWidth, targetHeight), samplingOptions);
            SKBitmap bitmapToEncode = resized ?? original;

            return EncodeToBitmapSource(bitmapToEncode);
        }

        private static BitmapSource? RenderSvgThumbnail(IImageSource source, int decodePixelWidth)
        {
            using Stream stream = source.OpenRead();
            using SKSvg svg = new();

            SKPicture? picture = svg.Load(stream);
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

            using SKImage snapshot = surface.Snapshot();
            using SKBitmap skBitmap = SKBitmap.FromImage(snapshot);
            return EncodeToBitmapSource(skBitmap);
        }

        internal static BitmapSource RenderThumbnailFromSvgText(string svgText, double viewBoxX, double viewBoxY, double viewBoxWidth, double viewBoxHeight)
        {
            using SKSvg svg = new();

            using MemoryStream mStream = new(Encoding.UTF8.GetBytes(svgText));
            SKPicture picture = svg.Load(mStream) ?? throw new InvalidOperationException("Svg.Skia не смог загрузить SVG для рендера превью.");
            SKRect pictureBounds = picture.CullRect;

            double sourceWidth = viewBoxWidth > 0 ? viewBoxWidth : pictureBounds.Width;
            double sourceHeight = viewBoxHeight > 0 ? viewBoxHeight : pictureBounds.Height;
            double sourceX = viewBoxWidth > 0 ? viewBoxX : pictureBounds.Left;
            double sourceY = viewBoxHeight > 0 ? viewBoxY : pictureBounds.Top;

            if (double.IsNaN(sourceWidth) || sourceWidth <= 0)
            {
                sourceWidth = DefaultThumbnailSize;
            }

            if (double.IsNaN(sourceHeight) || sourceHeight <= 0)
            {
                sourceHeight = DefaultThumbnailSize;
            }

            float scale = (float)(DefaultThumbnailSize / Math.Max(sourceWidth, sourceHeight));

            using SKBitmap bitmap = new(DefaultThumbnailSize, DefaultThumbnailSize, SKColorType.Rgba8888, SKAlphaType.Premul);
            using SKCanvas canvas = new(bitmap);
            canvas.Clear(SKColors.Transparent);
            canvas.Scale(scale);
            canvas.Translate((float)-sourceX, (float)-sourceY);
            canvas.DrawPicture(picture);
            canvas.Flush();

            return EncodeToBitmapSource(bitmap);
        }

        private static BitmapSource EncodeToBitmapSource(SKBitmap skBitmap)
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