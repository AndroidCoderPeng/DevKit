using System;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using DevKit.Cache;

namespace DevKit.Dialogs
{
    public partial class CommandScriptDialog
    {
        private ExCommandConfig _draggedCommand;
        private Point _dragStartPoint;

        public CommandScriptDialog()
        {
            InitializeComponent();
        }

        private void SelectedCommandListBox_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            _draggedCommand = null;

            if (sender is not ListBox listBox || e.OriginalSource is not DependencyObject originalSource)
                return;

            var item = ItemsControl.ContainerFromElement(listBox, originalSource) as ListBoxItem;
            _draggedCommand = item?.Content as ExCommandConfig;
            if (_draggedCommand != null)
            {
                _dragStartPoint = e.GetPosition(listBox);
            }
        }

        private void SelectedCommandListBox_PreviewMouseMove(object sender, MouseEventArgs e)
        {
            if (sender is not ListBox listBox || e.LeftButton != MouseButtonState.Pressed || _draggedCommand == null)
                return;

            var currentPoint = e.GetPosition(listBox);
            var horizontalDistance = Math.Abs(currentPoint.X - _dragStartPoint.X);
            var verticalDistance = Math.Abs(currentPoint.Y - _dragStartPoint.Y);
            if (horizontalDistance < SystemParameters.MinimumHorizontalDragDistance &&
                verticalDistance < SystemParameters.MinimumVerticalDragDistance)
                return;

            var draggedCommand = _draggedCommand;
            try
            {
                var data = new DataObject(typeof(ExCommandConfig), draggedCommand);
                DragDrop.DoDragDrop(listBox, data, DragDropEffects.Move);
            }
            finally
            {
                _draggedCommand = null;
            }
        }

        private void SelectedCommandListBox_DragOver(object sender, DragEventArgs e)
        {
            if (sender is ListBox listBox &&
                listBox.ItemsSource is ObservableCollection<ExCommandConfig> items &&
                e.Data.GetDataPresent(typeof(ExCommandConfig)) &&
                items.Contains(e.Data.GetData(typeof(ExCommandConfig)) as ExCommandConfig))
            {
                e.Effects = DragDropEffects.Move;
            }
            else
            {
                e.Effects = DragDropEffects.None;
            }

            e.Handled = true;
        }

        private void SelectedCommandListBox_Drop(object sender, DragEventArgs e)
        {
            e.Handled = true;

            if (sender is not ListBox listBox ||
                listBox.ItemsSource is not ObservableCollection<ExCommandConfig> items ||
                !e.Data.GetDataPresent(typeof(ExCommandConfig)) ||
                e.Data.GetData(typeof(ExCommandConfig)) is not ExCommandConfig draggedCommand ||
                e.OriginalSource is not DependencyObject targetItem)
                return;

            var targetContainer = ItemsControl.ContainerFromElement(listBox, targetItem) as ListBoxItem;
            var targetCommand = targetContainer?.Content as ExCommandConfig;
            if (targetCommand == null || ReferenceEquals(draggedCommand, targetCommand))
                return;

            var oldIndex = items.IndexOf(draggedCommand);
            var newIndex = items.IndexOf(targetCommand);

            if (oldIndex >= 0 && newIndex >= 0 && oldIndex != newIndex)
                items.Move(oldIndex, newIndex);
        }

        private void SelectedCommandListBox_PreviewMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            _draggedCommand = null;
        }
    }
}