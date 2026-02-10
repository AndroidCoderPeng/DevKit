using System.Collections.Specialized;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Microsoft.Xaml.Behaviors;

namespace DevKit.Behaviors
{
    public class AutoScrollToBottomBehavior : Behavior<ListBox>
    {
        /// <summary>
        /// 当ListBox的ItemsSource集合发生变化时触发
        /// </summary>
        protected override void OnAttached()
        {
            base.OnAttached();
            if (AssociatedObject.ItemsSource is INotifyCollectionChanged itemsSource)
            {
                // 订阅集合的CollectionChanged事件
                itemsSource.CollectionChanged += ItemsSource_CollectionChanged;
            }
            else
            {
                // 如果ItemsSource为空或不是INotifyCollectionChanged，则监听Items的CollectionChanged事件（对于直接添加到Items的情况）
                ((INotifyCollectionChanged)AssociatedObject.Items).CollectionChanged += Items_CollectionChanged;
            }
        }

        /// <summary>
        /// 当行为被移除时触发
        /// </summary>
        protected override void OnDetaching()
        {
            base.OnDetaching();
            if (AssociatedObject.ItemsSource is INotifyCollectionChanged itemsSource)
            {
                itemsSource.CollectionChanged -= ItemsSource_CollectionChanged;
            }
            else
            {
                ((INotifyCollectionChanged)AssociatedObject.Items).CollectionChanged -= Items_CollectionChanged;
            }
        }

        // 处理ItemsSource的CollectionChanged事件
        private void ItemsSource_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            // 检查是否添加了新项（例如，当e.Action == NotifyCollectionChangedAction.Add时）
            if (e.Action == NotifyCollectionChangedAction.Add || e.Action == NotifyCollectionChangedAction.Reset)
            {
                ScrollToBottom();
            }
        }

        // 处理直接添加到Items的CollectionChanged事件
        private void Items_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            ItemsSource_CollectionChanged(sender, e);
        }

        private void ScrollToBottom()
        {
            // 查找ListBox内部的ScrollViewer
            var scrollViewer = FindScrollViewer(AssociatedObject);
            scrollViewer?.ScrollToVerticalOffset(scrollViewer.ExtentHeight);
        }
        
        private ScrollViewer FindScrollViewer(DependencyObject parent)
        {
            if (parent == null) return null;
 
            ScrollViewer foundScrollViewer = null;
 
            for (var i = 0; i < VisualTreeHelper.GetChildrenCount(parent); i++)
            {
                var child = VisualTreeHelper.GetChild(parent, i);
                if (child is ScrollViewer scrollViewer)
                {
                    foundScrollViewer = scrollViewer;
                    break;
                }

                foundScrollViewer = FindScrollViewer(child);
                if (foundScrollViewer != null) break;
            }
 
            return foundScrollViewer;
        }
    }
}