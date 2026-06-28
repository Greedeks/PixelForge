using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;

namespace PixelForge.Assets.UserControls
{
    public partial class SvgPreviewPanel : UserControl
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

        public static readonly DependencyProperty ThumbnailProperty =
            DependencyProperty.Register(nameof(Thumbnail), typeof(BitmapSource), typeof(SvgPreviewPanel), new PropertyMetadata(null, OnThumbnailChanged));

        public static readonly DependencyProperty DesignedSizeTextProperty =
            DependencyProperty.Register(nameof(DesignedSizeText), typeof(string), typeof(SvgPreviewPanel), new PropertyMetadata("—"));

        public static readonly DependencyProperty ViewBoxTextProperty =
            DependencyProperty.Register(nameof(ViewBoxText), typeof(string), typeof(SvgPreviewPanel), new PropertyMetadata("—"));

        public static readonly DependencyProperty IsGuidesVisibleProperty =
            DependencyProperty.Register(nameof(IsGuidesVisible), typeof(bool), typeof(SvgPreviewPanel), new PropertyMetadata(false));

        public static readonly DependencyProperty ImageStretchProperty =
            DependencyProperty.Register(nameof(ImageStretch), typeof(Stretch), typeof(SvgPreviewPanel), new PropertyMetadata(Stretch.Uniform));

        public static readonly DependencyProperty XamlCodeProperty =
            DependencyProperty.Register(nameof(XamlCode), typeof(string), typeof(SvgPreviewPanel), new PropertyMetadata(string.Empty, OnCodeChanged));

        public static readonly DependencyProperty SvgCodeProperty =
            DependencyProperty.Register(nameof(SvgCode), typeof(string), typeof(SvgPreviewPanel), new PropertyMetadata(string.Empty, OnCodeChanged));

        public static readonly DependencyProperty IsXamlTabActiveProperty =
            DependencyProperty.Register(nameof(IsXamlTabActive), typeof(bool), typeof(SvgPreviewPanel), new PropertyMetadata(true));

        public BitmapSource Thumbnail
        {
            get => (BitmapSource)GetValue(ThumbnailProperty);
            set => SetValue(ThumbnailProperty, value);
        }

        public string DesignedSizeText
        {
            get => (string)GetValue(DesignedSizeTextProperty);
            private set => SetValue(DesignedSizeTextProperty, value);
        }

        public string ViewBoxText
        {
            get => (string)GetValue(ViewBoxTextProperty);
            set => SetValue(ViewBoxTextProperty, value);
        }

        public bool IsGuidesVisible
        {
            get => (bool)GetValue(IsGuidesVisibleProperty);
            set => SetValue(IsGuidesVisibleProperty, value);
        }

        public Stretch ImageStretch
        {
            get => (Stretch)GetValue(ImageStretchProperty);
            set => SetValue(ImageStretchProperty, value);
        }

        public string XamlCode
        {
            get => (string)GetValue(XamlCodeProperty);
            set => SetValue(XamlCodeProperty, value);
        }

        public string SvgCode
        {
            get => (string)GetValue(SvgCodeProperty);
            set => SetValue(SvgCodeProperty, value);
        }

        public bool IsXamlTabActive
        {
            get => (bool)GetValue(IsXamlTabActiveProperty);
            private set => SetValue(IsXamlTabActiveProperty, value);
        }

        public SvgPreviewPanel()
        {
            InitializeComponent();
            UpdateCodeView();
        }

        private static void OnThumbnailChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) => ((SvgPreviewPanel)d).UpdateDesignedSizeText();

        private static void OnCodeChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) => ((SvgPreviewPanel)d).UpdateCodeView();

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

            if (!string.IsNullOrEmpty(text))
            {
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
        }

        private void ShowCopiedToast()
        {
            _toastStoryboard?.Stop();

            CopiedToast.Opacity = 0;
            ToastTranslate.Y = 6;

            Storyboard storyboard = _toastStoryboard = new();

            FactoryAnimation(_opacityPropertyPath, 0, 1, 120, 0, EasingMode.EaseOut);
            FactoryAnimation(_translateYPropertyPath, 6, 0, 150, 0, EasingMode.EaseOut);

            FactoryAnimation(_opacityPropertyPath, 1, 0, 220, 1200, EasingMode.EaseIn);
            FactoryAnimation(_translateYPropertyPath, 0, 4, 220, 1200, EasingMode.EaseIn);

            storyboard.Begin(CopiedToast);

            void FactoryAnimation(PropertyPath property, double from, double to, int duration, int delay, EasingMode easingMode)
            {
                DoubleAnimation animation = new(from, to, TimeSpan.FromMilliseconds(duration))
                {
                    BeginTime = TimeSpan.FromMilliseconds(delay),
                    EasingFunction = new CubicEase { EasingMode = easingMode }
                };

                Storyboard.SetTargetProperty(animation, property);
                storyboard.Children.Add(animation);
            }
        }

        private void UpdateDesignedSizeText()
        {
            if (Thumbnail is null)
            {
                DesignedSizeText = "—";
                return;
            }

            DesignedSizeText = $"{Thumbnail.PixelWidth} x {Thumbnail.PixelHeight}";
        }

        private void UpdateCodeView()
        {
            if (CodeTextBox != null)
            {
                CodeTextBox.Code = IsXamlTabActive ? XamlCode ?? string.Empty : SvgCode ?? string.Empty;
            }
        }
    }
}
