using System.Diagnostics;
using System.Text;
using System.Windows;

namespace PixelForge.Windows
{
    public enum MessageWindowType
    {
        ErrorSave,
        ErrorOptimize,
        UnknownError
    }

    public partial class MessageWindow : Window
    {
        private string? _exceptionDetails;

        public MessageWindow(Exception ex, MessageWindowType type = MessageWindowType.ErrorSave)
        {
            InitializeComponent();
            InitializeMessageContent(ex, type);
        }

        private void InitializeMessageContent(Exception ex, MessageWindowType type)
        {
            Title += GetWindowTitle(type);

            if (ex is null)
            {
                return;
            }

            StringBuilder sb = new();

            int index = 1;
            AppendException(sb, ex, ref index);

            _exceptionDetails = sb.ToString();
            TxtErrorDetails.Text = _exceptionDetails;
        }
        private static void AppendException(StringBuilder sb, Exception ex, ref int index)
        {
            if (index > 1)
            {
                sb.AppendLine($"{new string('-', 40)}\n");
            }

            sb.AppendLine($"[{index++}] {ex.GetType().FullName}");
            sb.AppendLine($"    Message: {ex.Message}");
            sb.AppendLine($"    Type: {ex.GetType().FullName}");

            if (!string.IsNullOrWhiteSpace(ex.StackTrace))
            {
                sb.AppendLine("     Stack Trace:");
                foreach (var line in ex.StackTrace.Split('\n'))
                {
                    sb.AppendLine($"    {line.TrimStart()}");
                }
            }

            sb.AppendLine();

            if (ex.InnerException is not null)
            {
                AppendException(sb, ex.InnerException, ref index);
            }
        }

        private string GetWindowTitle(MessageWindowType type)
        {
            return $" - {type switch
            {
                MessageWindowType.ErrorSave => TryFindResource("msg_SaveError") as string ?? string.Empty,
                MessageWindowType.ErrorOptimize => TryFindResource("msg_OptimizationError") as string ?? string.Empty,
                _ => "Unknown Error"
            }}";
        }

        private async void BtnCopy_Click(object sender, RoutedEventArgs e)
        {
            Thread thread = new(() =>
            {
                try { Clipboard.SetText(_exceptionDetails); }
                catch (Exception ex) { Debug.WriteLine(ex); }
            });
            thread.SetApartmentState(ApartmentState.STA);
            thread.IsBackground = true;
            thread.Start();
        }

        private void BtnClose_Click(object sender, RoutedEventArgs e) => Close();
    }
}