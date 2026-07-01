using System.Windows;
using System.Windows.Controls;
using PixelForge.Core.ViewModel;

namespace PixelForge.View
{
    public partial class OptimizerView : UserControl
    {
        private OptimizerViewModel? ViewModel => DataContext as OptimizerViewModel;

        public OptimizerView() => InitializeComponent();

        private void OnDragOver(object sender, DragEventArgs e)
        {
            e.Effects = e.Data.GetDataPresent(DataFormats.FileDrop) ? DragDropEffects.Copy : DragDropEffects.None;
            e.Handled = true;
        }

        private void OnDrop(object sender, DragEventArgs e)
        {
            if (e.Data.GetData(DataFormats.FileDrop) is string[] paths)
            {
                ViewModel?.AddPaths(paths);
            }
        }

        private void OnDropZoneClick(object sender, System.Windows.Input.MouseButtonEventArgs e) => ViewModel?.AddFilesCommand.Execute(null);
    }

}

