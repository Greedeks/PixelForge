using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media;

namespace PixelForge.Helpers.Interop
{
    internal class WindowAttributes
    {
        private const int DWMWA_CAPTION_COLOR = 35;
        private const int DWMWA_TEXT_COLOR = 36;

        [DllImport("dwmapi.dll")]
        private static extern int DwmSetWindowAttribute(IntPtr hwnd, int dwAttribute, ref int pvAttribute, int cbAttribute);

        internal static readonly DependencyProperty CaptionColorProperty =
            DependencyProperty.RegisterAttached("CaptionColor", typeof(object), typeof(WindowAttributes), new PropertyMetadata(Colors.Transparent, OnCaptionColorChanged));

        internal static object GetCaptionColor(DependencyObject obj) => obj.GetValue(CaptionColorProperty);

        internal static void SetCaptionColor(DependencyObject obj, object value) => obj.SetValue(CaptionColorProperty, value);

        internal static readonly DependencyProperty TextColorProperty =
            DependencyProperty.RegisterAttached("TextColor", typeof(object), typeof(WindowAttributes), new PropertyMetadata(Colors.White, OnTextColorChanged));

        internal static object GetTextColor(DependencyObject obj) => obj.GetValue(TextColorProperty);

        internal static void SetTextColor(DependencyObject obj, object value) => obj.SetValue(TextColorProperty, value);

        private static void OnCaptionColorChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is not Window window)
            {
                return;
            }

            Apply(window);
        }

        private static void OnTextColorChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is not Window window)
            {
                return;
            }

            Apply(window);
        }

        private static void Apply(Window window)
        {
            void SetAttributes()
            {
                IntPtr hwnd = new WindowInteropHelper(window).Handle;
                if (hwnd == IntPtr.Zero)
                {
                    return;
                }

                Color captionColor = ResolveColor(GetCaptionColor(window), Colors.Transparent);
                int captionRef = ToColorRef(captionColor);

                _ = DwmSetWindowAttribute(hwnd, DWMWA_CAPTION_COLOR, ref captionRef, sizeof(int));

                Color textColor = ResolveColor(GetTextColor(window), Colors.White);
                int textRef = ToColorRef(textColor);

                _ = DwmSetWindowAttribute(hwnd, DWMWA_TEXT_COLOR, ref textRef, sizeof(int));
            }

            if (window.IsInitialized)
            {
                SetAttributes();
            }
            else
            {
                window.SourceInitialized += (_, _) => SetAttributes();
            }
        }

        private static int ToColorRef(Color color) => color.R | (color.G << 8) | (color.B << 16);

        private static Color ResolveColor(object value, Color fallback)
        {
            return value switch
            {
                Color color => color,
                SolidColorBrush solidBrush => solidBrush.Color,
                GradientBrush gradientBrush => AverageGradientColor(gradientBrush, fallback),
                Brush _ => fallback,
                _ => fallback,
            };
        }

        private static Color AverageGradientColor(GradientBrush brush, Color fallback)
        {
            if (brush.GradientStops == null || brush.GradientStops.Count == 0)
            {
                return fallback;
            }

            int count = brush.GradientStops.Count;
            int a = 0, r = 0, g = 0, b = 0;

            foreach (GradientStop stop in brush.GradientStops)
            {
                a += stop.Color.A;
                r += stop.Color.R;
                g += stop.Color.G;
                b += stop.Color.B;
            }

            return Color.FromArgb((byte)(a / count), (byte)(r / count), (byte)(g / count), (byte)(b / count));
        }
    }
}