using System.Globalization;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace PixelForge.Core.Converters
{
    public class ImageToCheckerboardConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            if (values[0] is BitmapSource bitmap)
            {
                double lum = GetAverageLuminance(bitmap);
                string key = lum > 0.5 ? "CheckerboardDark" : "CheckerboardLight";
                return Application.Current.Resources[key] ?? DependencyProperty.UnsetValue;
            }
            return DependencyProperty.UnsetValue;
        }

        private static double GetAverageLuminance(BitmapSource source)
        {
            double scaleX = Math.Min(32.0 / source.PixelWidth, 1.0);
            double scaleY = Math.Min(32.0 / source.PixelHeight, 1.0);
            TransformedBitmap scaled = new(source, new ScaleTransform(scaleX, scaleY));
            FormatConvertedBitmap converted = new(scaled, PixelFormats.Bgra32, null, 0);

            int w = converted.PixelWidth, h = converted.PixelHeight;
            int stride = w * 4;
            byte[] px = new byte[h * stride];
            converted.CopyPixels(px, stride, 0);

            double total = 0;
            int count = 0;

            for (int i = 0; i < px.Length; i += 4)
            {
                byte a = px[i + 3];
                if (a < 10)
                {
                    continue;
                }

                double r = px[i + 2] / 255.0;
                double g = px[i + 1] / 255.0;
                double b = px[i + 0] / 255.0;

                total += 0.2126 * r + 0.7152 * g + 0.0722 * b;
                count++;
            }

            return count > 0 ? total / count : 0.5;
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture) => throw new NotImplementedException();
    }
}
