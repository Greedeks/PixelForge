using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Imaging;

namespace PixelForge.Assets.UserControls
{
    public partial class FileCard : UserControl
    {
        internal static readonly DependencyProperty FileNameProperty =
            DependencyProperty.Register(nameof(FileName), typeof(string), typeof(FileCard), new PropertyMetadata(string.Empty));

        internal static readonly DependencyProperty ThumbnailProperty =
            DependencyProperty.Register(nameof(Thumbnail), typeof(BitmapSource), typeof(FileCard), new PropertyMetadata(null));

        internal static readonly DependencyProperty FileSizeBeforeProperty =
            DependencyProperty.Register(nameof(FileSizeBefore), typeof(string), typeof(FileCard), new PropertyMetadata(string.Empty));

        internal static readonly DependencyProperty FileSizeAfterProperty =
            DependencyProperty.Register(nameof(FileSizeAfter), typeof(string), typeof(FileCard), new PropertyMetadata(string.Empty));

        internal static readonly DependencyProperty SavedPercentProperty =
            DependencyProperty.Register(nameof(SavedPercent), typeof(string), typeof(FileCard), new PropertyMetadata(string.Empty));

        internal static readonly DependencyProperty IsProcessingProperty =
            DependencyProperty.Register(nameof(IsProcessing), typeof(bool), typeof(FileCard), new PropertyMetadata(false));

        internal static readonly DependencyProperty IsDoneProperty =
            DependencyProperty.Register(nameof(IsDone), typeof(bool), typeof(FileCard), new PropertyMetadata(false));

        internal static readonly DependencyProperty IsSavedProperty =
            DependencyProperty.Register(nameof(IsSaved), typeof(bool), typeof(FileCard), new PropertyMetadata(false));

        internal string FileName
        {
            get => (string)GetValue(FileNameProperty);
            set => SetValue(FileNameProperty, value);
        }

        internal BitmapSource? Thumbnail
        {
            get => (BitmapSource?)GetValue(ThumbnailProperty);
            set => SetValue(ThumbnailProperty, value);
        }

        internal string FileSizeBefore
        {
            get => (string)GetValue(FileSizeBeforeProperty);
            set => SetValue(FileSizeBeforeProperty, value);
        }

        internal string FileSizeAfter
        {
            get => (string)GetValue(FileSizeAfterProperty);
            set => SetValue(FileSizeAfterProperty, value);
        }

        internal string SavedPercent
        {
            get => (string)GetValue(SavedPercentProperty);
            set => SetValue(SavedPercentProperty, value);
        }

        internal bool IsProcessing
        {
            get => (bool)GetValue(IsProcessingProperty);
            set => SetValue(IsProcessingProperty, value);
        }

        internal bool IsDone
        {
            get => (bool)GetValue(IsDoneProperty);
            set => SetValue(IsDoneProperty, value);
        }

        internal bool IsSaved
        {
            get => (bool)GetValue(IsSavedProperty);
            set => SetValue(IsSavedProperty, value);
        }

        public FileCard()
        {
            InitializeComponent();
        }
    }
}