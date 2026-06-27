using System.Windows.Media.Imaging;
using PixelForge.Core.Base;
using PixelForge.Core.Providers.Images;
using PixelForge.Helpers;

namespace PixelForge.Core.Model
{
    internal class FileCardModel(OptimizerModel model, IImageSource source, BitmapSource? thumbnail = null) : ViewModelBase
    {
        private bool _isProcessing;
        private bool _isDone;
        private bool _isSaved;
        private bool _hasError;

        public OptimizerModel Model { get; } = model;
        public IImageSource Source { get; } = source;
        public BitmapSource? Thumbnail { get; init; } = thumbnail;

        public string FileName => Model.FileName;
        public string FileSizeBefore => FileSizeFormatter.Format(Model.SizeBytes);
        public string FileSizeAfter => Model.OptimizationResult is { } r ? FileSizeFormatter.Format(r.SizeAfterBytes) : string.Empty;

        public string SavedPercent => Model.OptimizationResult is { } r ? $" (-{r.SavedPercent:0}%)" : string.Empty;

        public bool IsProcessing
        {
            get => _isProcessing;
            set { _isProcessing = value; OnPropertyChanged(); }
        }
        public bool IsDone
        {
            get => _isDone;
            set { _isDone = value; OnPropertyChanged(); }
        }

        public bool IsSaved
        {
            get => _isSaved;
            set
            {
                if (_isSaved == value)
                {
                    return;
                }

                _isSaved = value;
                OnPropertyChanged();
            }
        }

        public bool HasError
        {
            get => _hasError;
            set { _hasError = value; OnPropertyChanged(); }
        }

        public void ApplyResult(OptimizationResult result)
        {
            Model.OptimizationResult = result;
            OnPropertyChanged(nameof(FileSizeAfter));
            OnPropertyChanged(nameof(SavedPercent));
            IsDone = true;
        }
    }
}