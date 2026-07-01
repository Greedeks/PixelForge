using System.Buffers;
using System.Globalization;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace PixelForge.Core.Converters
{
    public sealed class ImageToCheckerboardConverter : IMultiValueConverter
    {
        private const int SampleSize = 32;
        private const byte AlphaThreshold = 10;
        private const double LuminanceThreshold = 0.5;

        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            if (values.Length == 0 || values[0] is not BitmapSource bitmap)
            {
                return DependencyProperty.UnsetValue;
            }

            double luminance = GetAverageLuminance(bitmap);

            return Application.Current.Resources[luminance > LuminanceThreshold ? "CheckerboardDark" : "CheckerboardLight"];
        }

        private static double GetAverageLuminance(BitmapSource source)
        {
            BitmapSource bitmap = source;

            if (bitmap.PixelWidth > SampleSize || bitmap.PixelHeight > SampleSize)
            {
                double scale = Math.Min((double)SampleSize / bitmap.PixelWidth, (double)SampleSize / bitmap.PixelHeight);

                bitmap = new TransformedBitmap(bitmap, new ScaleTransform(scale, scale));
            }

            if (bitmap.Format != PixelFormats.Bgra32)
            {
                bitmap = new FormatConvertedBitmap(bitmap, PixelFormats.Bgra32, null, 0);
            }

            int stride = bitmap.PixelWidth * 4;
            int requiredSize = stride * bitmap.PixelHeight;
            byte[] pixels = ArrayPool<byte>.Shared.Rent(requiredSize);

            try
            {
                bitmap.CopyPixels(pixels, stride, 0);

                double total = 0;
                double weight = 0;

                Span<byte> span = pixels.AsSpan(0, requiredSize);

                for (int i = 0; i < span.Length; i += 4)
                {
                    byte rawAlpha = span[i + 3];
                    if (rawAlpha < AlphaThreshold)
                    {
                        continue;
                    }
                    double alpha = rawAlpha / 255.0;

                    double luminance = span[i + 2] * 0.2126 + span[i + 1] * 0.7152 + span[i + 0] * 0.0722;

                    total += luminance * alpha;
                    weight += alpha;
                }

                return weight > 0 ? total / (255.0 * weight) : 0.5;
            }
            finally { ArrayPool<byte>.Shared.Return(pixels); }
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture) => throw new NotSupportedException();
    }
}