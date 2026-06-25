using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace PixelForge.Core.Converters
{
    public class WaitingStateToVisibilityConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            bool isDone = values.Length > 0 && values[0] is true;
            bool isProcessing = values.Length > 1 && values[1] is true;
            return (!isDone && !isProcessing) ? Visibility.Visible : Visibility.Collapsed;
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture) => throw new NotSupportedException();
    }
}
