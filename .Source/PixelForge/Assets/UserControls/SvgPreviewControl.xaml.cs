using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;

namespace PixelForge.Assets.UserControls
{
    public partial class SvgPreviewControl : UserControl
    {
        private static readonly Stretch[] StretchModes =
        [
            Stretch.Uniform,
            Stretch.UniformToFill,
            Stretch.Fill,
            Stretch.None
        ];

        private Storyboard? _toastStoryboard;
        private static readonly PropertyPath _opacityPropertyPath = new(OpacityProperty);
        private static readonly PropertyPath _translateYPropertyPath = new("(UIElement.RenderTransform).(TranslateTransform.Y)");

        internal static readonly DependencyProperty DrawingImageProperty =
            DependencyProperty.Register(nameof(DrawingImage), typeof(DrawingImage), typeof(SvgPreviewControl), new PropertyMetadata(null));

        internal static readonly DependencyProperty ThumbnailProperty =
            DependencyProperty.Register(nameof(Thumbnail), typeof(BitmapSource), typeof(SvgPreviewControl), new PropertyMetadata(null));

        internal static readonly DependencyProperty DesignedSizeTextProperty =
            DependencyProperty.Register(nameof(DesignedSizeText), typeof(string), typeof(SvgPreviewControl), new PropertyMetadata("—"));

        internal static readonly DependencyProperty ViewBoxTextProperty =
            DependencyProperty.Register(nameof(ViewBoxText), typeof(string), typeof(SvgPreviewControl), new PropertyMetadata("—"));

        internal static readonly DependencyProperty IsGuidesVisibleProperty =
            DependencyProperty.Register(nameof(IsGuidesVisible), typeof(bool), typeof(SvgPreviewControl), new PropertyMetadata(false));

        internal static readonly DependencyProperty ImageStretchProperty =
            DependencyProperty.Register(nameof(ImageStretch), typeof(Stretch), typeof(SvgPreviewControl), new PropertyMetadata(Stretch.Uniform));

        internal static readonly DependencyProperty XamlCodeProperty =
            DependencyProperty.Register(nameof(XamlCode), typeof(string), typeof(SvgPreviewControl), new PropertyMetadata(string.Empty, OnCodeChanged));

        internal static readonly DependencyProperty SvgCodeProperty =
            DependencyProperty.Register(nameof(SvgCode), typeof(string), typeof(SvgPreviewControl), new PropertyMetadata(string.Empty, OnCodeChanged));

        internal static readonly DependencyProperty IsXamlTabActiveProperty =
            DependencyProperty.Register(nameof(IsXamlTabActive), typeof(bool), typeof(SvgPreviewControl), new PropertyMetadata(true));

        internal static readonly DependencyProperty IsErrorProperty =
            DependencyProperty.Register(nameof(IsError), typeof(bool), typeof(SvgPreviewControl), new PropertyMetadata(false));

        internal DrawingImage? DrawingImage
        {
            get => (DrawingImage?)GetValue(DrawingImageProperty);
            set => SetValue(DrawingImageProperty, value);
        }

        internal BitmapSource? Thumbnail
        {
            get => (BitmapSource?)GetValue(ThumbnailProperty);
            set => SetValue(ThumbnailProperty, value);
        }

        internal string DesignedSizeText
        {
            get => (string)GetValue(DesignedSizeTextProperty);
            set => SetValue(DesignedSizeTextProperty, value);
        }

        internal string ViewBoxText
        {
            get => (string)GetValue(ViewBoxTextProperty);
            set => SetValue(ViewBoxTextProperty, value);
        }

        internal bool IsGuidesVisible
        {
            get => (bool)GetValue(IsGuidesVisibleProperty);
            set => SetValue(IsGuidesVisibleProperty, value);
        }

        internal Stretch ImageStretch
        {
            get => (Stretch)GetValue(ImageStretchProperty);
            set => SetValue(ImageStretchProperty, value);
        }

        internal string XamlCode
        {
            get => (string)GetValue(XamlCodeProperty);
            set => SetValue(XamlCodeProperty, value);
        }

        internal string SvgCode
        {
            get => (string)GetValue(SvgCodeProperty);
            set => SetValue(SvgCodeProperty, value);
        }

        internal bool IsXamlTabActive
        {
            get => (bool)GetValue(IsXamlTabActiveProperty);
            private set => SetValue(IsXamlTabActiveProperty, value);
        }

        internal bool IsError
        {
            get => (bool)GetValue(IsErrorProperty);
            set => SetValue(IsErrorProperty, value);
        }

        public SvgPreviewControl()
        {
            InitializeComponent();
            UpdateCodeView();
        }

        private static void OnCodeChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) => ((SvgPreviewControl)d).UpdateCodeView();

        private void Tab_Checked(object sender, RoutedEventArgs e)
        {
            IsXamlTabActive = sender == TabXaml;
            UpdateCodeView();
        }

        private void BtnGuides_MouseLeftButtonUp(object sender, MouseButtonEventArgs e) => IsGuidesVisible = !IsGuidesVisible;

        private void StretchToggle_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            int index = (Array.IndexOf(StretchModes, ImageStretch) + 1) % StretchModes.Length;
            ImageStretch = StretchModes[index];
        }

        private void BtnCopy_Click(object sender, RoutedEventArgs e)
        {
            string text = IsXamlTabActive ? XamlCode : SvgCode;
            if (string.IsNullOrEmpty(text))
            {
                return;
            }

            Thread thread = new(() =>
            {
                try { Clipboard.SetText(text); }
                catch (Exception ex) { Debug.WriteLine(ex); }
            });
            thread.SetApartmentState(ApartmentState.STA);
            thread.IsBackground = true;
            thread.Start();
            ShowCopiedToast();
        }

        private void ShowCopiedToast()
        {
            _toastStoryboard?.Stop();
            CopiedToast.Opacity = 0;
            ToastTranslate.Y = 6;

            Storyboard sb = _toastStoryboard = new();
            FactoryAnimation(_opacityPropertyPath, 0, 1, 120, 0, EasingMode.EaseOut);
            FactoryAnimation(_translateYPropertyPath, 6, 0, 150, 0, EasingMode.EaseOut);
            FactoryAnimation(_opacityPropertyPath, 1, 0, 220, 1200, EasingMode.EaseIn);
            FactoryAnimation(_translateYPropertyPath, 0, 4, 220, 1200, EasingMode.EaseIn);
            sb.Begin(CopiedToast);

            void FactoryAnimation(PropertyPath prop, double from, double to, int ms, int delay, EasingMode ease)
            {
                DoubleAnimation doubleAnim = new(from, to, TimeSpan.FromMilliseconds(ms))
                {
                    BeginTime = TimeSpan.FromMilliseconds(delay),
                    EasingFunction = new CubicEase { EasingMode = ease }
                };
                Storyboard.SetTargetProperty(doubleAnim, prop);
                sb.Children.Add(doubleAnim);
            }
        }

        private void UpdateCodeView()
        {
            if (CodeTextBox is null)
            {
                return;
            }

            CodeTextBox.Code = IsXamlTabActive ? XamlCode ?? string.Empty : SvgCode ?? string.Empty;
        }
    }
}