using System;
using System.Windows;
using System.Windows.Controls;

namespace DevKit.Behaviors
{
    internal class ListBoxItemMarginBehavior
    {
        public static readonly DependencyProperty ApplyCustomMarginProperty =
            DependencyProperty.RegisterAttached(
                "ApplyCustomMargin",
                typeof(bool),
                typeof(ListBoxItemMarginBehavior),
                new PropertyMetadata(false, OnApplyCustomMarginChanged));

        public static bool GetApplyCustomMargin(DependencyObject obj) =>
            (bool)obj.GetValue(ApplyCustomMarginProperty);

        public static void SetApplyCustomMargin(DependencyObject obj, bool value) =>
            obj.SetValue(ApplyCustomMarginProperty, value);

        private static void OnApplyCustomMarginChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is ListBoxItem item)
            {
                item.Loaded += (sender, args) =>
                {
                    if (!(ItemsControl.ItemsControlFromItemContainer(item) is ListBox listBox)) return;

                    var index = listBox.Items.IndexOf(item.Content);
                    var count = listBox.Items.Count;

                    // 获取 ListBox 和 ListBoxItem 的实际宽度（可能需要异步等待布局更新）
                    var listBoxWidth = listBox.ActualWidth;
                    var itemWidth = item.ActualWidth;
                    var itemsPerRow = (int)(listBoxWidth / itemWidth);

                    if (itemsPerRow <= 0) itemsPerRow = 3; // 默认每行3个

                    var row = index / itemsPerRow;
                    var totalRows = (int)Math.Ceiling(count / (double)itemsPerRow);

                    const int margin = 2;
                    double topMargin = row == 0 ? 0 : margin; // 第一行去掉上边距
                    double bottomMargin = row == totalRows - 1 ? 0 : margin; // 最后一行去掉下边距

                    if (index % itemsPerRow == 0)
                    {
                        item.Margin = new Thickness(0, topMargin, margin, bottomMargin); // 每行第一个去掉左间距
                    }
                    else if ((index + 1) % itemsPerRow == 0)
                    {
                        item.Margin = new Thickness(margin, topMargin, 0, bottomMargin); // 每行最后一个去掉右间距
                    }
                    else
                    {
                        item.Margin = new Thickness(margin, topMargin, margin, bottomMargin); // 中间项保留左右间距
                    }
                };
            }
        }
    }
}