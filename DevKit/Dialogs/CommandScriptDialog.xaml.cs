using System.Collections;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace DevKit.Dialogs
{
    public partial class CommandScriptDialog : UserControl
    {
        private object _draggedItem;
        private Point _dragStartPoint;

        public CommandScriptDialog()
        {
            InitializeComponent();
        }

        private void SelectedCommandListBox_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (!(sender is ListBox listBox)) return;
            _dragStartPoint = e.GetPosition(listBox);
            if (!(e.OriginalSource is DependencyObject originalSource)) return;
            if (ItemsControl.ContainerFromElement(listBox, originalSource) is ListBoxItem item)
            {
                _draggedItem = item.Content;
            }
        }

        private void SelectedCommandListBox_PreviewMouseMove(object sender, MouseEventArgs e)
        {
            if (!(sender is ListBox listBox)) return;
            if (e.LeftButton != MouseButtonState.Pressed || _draggedItem == null) return;
            var currentPoint = e.GetPosition(listBox);
            if ((currentPoint - _dragStartPoint).Length > SystemParameters.MinimumHorizontalDragDistance)
            {
                DragDrop.DoDragDrop(listBox, _draggedItem, DragDropEffects.Move);
            }
        }

        private void SelectedCommandListBox_DragOver(object sender, DragEventArgs e)
        {
            e.Effects = DragDropEffects.Move;
            e.Handled = true;
        }

        private void SelectedCommandListBox_Drop(object sender, DragEventArgs e)
        {
            if (!(sender is ListBox listBox)) return;
            var targetItem = e.OriginalSource as DependencyObject;
            if (targetItem == null) return;
            var targetContainer = ItemsControl.ContainerFromElement(listBox, targetItem) as ListBoxItem;
            if (targetContainer != null && _draggedItem != null && _draggedItem != targetContainer.Content)
            {
                if (!(listBox.ItemsSource is IList items)) return;
                var oldIndex = items.IndexOf(_draggedItem);
                var newIndex = items.IndexOf(targetContainer.Content);

                if (oldIndex >= 0 && newIndex >= 0)
                {
                    items.Remove(_draggedItem);
                    items.Insert(newIndex, _draggedItem);
                }
            }

            _draggedItem = null;
        }
    }
}