using System.Windows;

namespace PixelForge
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
        }

        protected override void OnSourceInitialized(EventArgs e)
        {
            base.OnSourceInitialized(e);

            Rect area = SystemParameters.WorkArea;

            double rawWidth = area.Width * (area.Width <= 1600 ? 0.82 : 0.62);
            Width = Math.Max(1150, Math.Min(rawWidth, 1500));

            if (Width > area.Width)
            {
                Width = area.Width * 0.96;
            }

            Height = Math.Min(Width / 1.8, area.Height * 0.90);

            Left = area.Left + (area.Width - Width) / 2;
            Top = area.Top + (area.Height - Height) / 2;
        }
    }
}