using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using DevKit.Cache;

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
            if (!(sender is ListBox listBox))
            {
                return;
            }

            if (!(e.OriginalSource is DependencyObject targetItem))
            {
                return;
            }

            var targetContainer = (ListBoxItem)ItemsControl.ContainerFromElement(listBox, targetItem);
            if (targetContainer != null && _draggedItem != null &&
                !ReferenceEquals(_draggedItem, targetContainer.Content) &&
                listBox.ItemsSource is ObservableCollection<ExCommandConfig> items)
            {
                var draggedCommand = (ExCommandConfig)_draggedItem;
                var targetCommand = (ExCommandConfig)targetContainer.Content;

                if (draggedCommand != null && targetCommand != null)
                {
                    var oldIndex = items.IndexOf(draggedCommand);
                    var newIndex = items.IndexOf(targetCommand);

                    if (oldIndex >= 0 && newIndex >= 0 && oldIndex != newIndex)
                    {
                        items.Move(oldIndex, newIndex);
                    }
                }
            }

            _draggedItem = null;
        }
    }
}