using System.Windows;
using PixelForge.Windows;

namespace PixelForge.Helpers.Managers
{
    internal sealed class MessageWindowManager
    {
        internal static void Show(Exception ex, MessageWindowType type = MessageWindowType.UnknownError) => Application.Current.Dispatcher.Invoke(() => new MessageWindow(ex, type) { Owner = Application.Current.MainWindow }.ShowDialog());

        internal static void ShowSaveError(Exception ex) => Show(ex, MessageWindowType.ErrorSave);

        internal static void ShowOptimizeError(Exception ex) => Show(ex, MessageWindowType.ErrorOptimize);

        internal static void ShowErrors(IEnumerable<Exception> exceptions, MessageWindowType type = MessageWindowType.UnknownError)
        {
            List<Exception> list = [.. exceptions];
            if (list.Count != 0)
            {
                Exception toShow = list.Count == 1 ? list[0] : new AggregateException($"Failed with {list.Count} error(s)", list);
                Show(toShow, type);
            }
        }
    }
}
