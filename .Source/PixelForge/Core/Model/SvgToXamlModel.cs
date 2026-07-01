using System.Windows.Media;
using System.Windows.Media.Imaging;
using PixelForge.Core.Base;
using PixelForge.Core.Providers.Images;

namespace PixelForge.Core.Model
{
    internal sealed class SvgToXamlModel(IImageSource source) : ViewModelBase
    {
        private SvgConversionResult? _result;
        private bool _isConverting;
        private bool _hasError;

        public string FileName => Source.Name;

        public BitmapSource? Thumbnail => _result?.Thumbnail;

        public DrawingImage? DrawingImage => _result?.DrawingImage;

        public IImageSource Source { get; } = source;

        public bool IsConverting
        {
            get => _isConverting;
            set { _isConverting = value; OnPropertyChanged(); }
        }

        public bool HasError
        {
            get => _hasError;
            set { _hasError = value; OnPropertyChanged(); }
        }

        public bool IsReady => Result is not null;

        public SvgConversionResult? Result
        {
            get => _result;
            set
            {
                _result = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(IsReady));
                OnPropertyChanged(nameof(Thumbnail));
                OnPropertyChanged(nameof(DrawingImage));
            }
        }
    }
}