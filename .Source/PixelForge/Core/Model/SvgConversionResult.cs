using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace PixelForge.Core.Model
{
    internal sealed class SvgConversionResult
    {
        public DrawingImage? DrawingImage { get; init; }

        public BitmapSource? Thumbnail { get; init; }

        public string XamlCode { get; init; } = string.Empty;

        public string SvgCode { get; init; } = string.Empty;

        public double ViewBoxX { get; init; }

        public double ViewBoxY { get; init; }

        public double ViewBoxWidth { get; init; }

        public double ViewBoxHeight { get; init; }

        public string ViewBoxText => $"{ViewBoxX:0} {ViewBoxY:0} {ViewBoxWidth:0} {ViewBoxHeight:0}";

        public string DesignedSizeText => $"{ViewBoxWidth:0} x {ViewBoxHeight:0}";

        public bool IsError { get; init; }
    }
}