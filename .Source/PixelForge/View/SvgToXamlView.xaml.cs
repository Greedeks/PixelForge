using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;
using PixelForge.Core.ViewModel;

namespace PixelForge.View
{
    public partial class SvgToXamlView : UserControl
    {
        private SvgToXamlViewModel? ViewModel => DataContext as SvgToXamlViewModel;

        public SvgToXamlView()
        {
            InitializeComponent();
            InitializeContextMenu(TabListToggle);
            InitializeContextMenu(DotsMenuBtn);
        }

        private static void InitializeContextMenu(ToggleButton button)
        {
            if (button.ContextMenu is null)
            {
                return;
            }

            button.ContextMenu.PlacementTarget = button;
            button.ContextMenu.Placement = PlacementMode.Bottom;
            button.ContextMenuOpening += (s, e) => e.Handled = true;
            button.ContextMenu.Closed += (_, _) => button.IsChecked = false;
            button.Checked += (_, _) => button.ContextMenu.IsOpen = true;
            button.Unchecked += (_, _) => button.ContextMenu.IsOpen = false;
        }

        private void OnDragOver(object sender, DragEventArgs e)
        {
            e.Effects = e.Data.GetDataPresent(DataFormats.FileDrop) ? DragDropEffects.Copy : DragDropEffects.None;
            e.Handled = true;
        }

        private void OnDrop(object sender, DragEventArgs e)
        {
            if (e.Data.GetData(DataFormats.FileDrop) is not string[] dropped)
            {
                return;
            }

            ViewModel?.AddPaths(dropped);
        }

        private void ListBox_PreviewMouseWheel(object sender, MouseWheelEventArgs e)
        {
            if (sender is ListBox listBox)
            {
                ScrollViewer? scrollViewer = listBox.Template.FindName("PART_ScrollViewer", listBox) as ScrollViewer ?? FindVisualChild<ScrollViewer>(listBox);

                if (scrollViewer is null)
                {
                    return;
                }

                scrollViewer.ScrollToHorizontalOffset(scrollViewer.HorizontalOffset - e.Delta);
                e.Handled = true;
            }
        }

        private static T? FindVisualChild<T>(DependencyObject parent) where T : DependencyObject
        {
            for (int i = 0; i < VisualTreeHelper.GetChildrenCount(parent); i++)
            {
                DependencyObject child = VisualTreeHelper.GetChild(parent, i);
                if (child is T t)
                {
                    return t;
                }

                T? result = FindVisualChild<T>(child);
                if (result is not null)
                {
                    return result;
                }
            }
            return null;
        }

    }
}