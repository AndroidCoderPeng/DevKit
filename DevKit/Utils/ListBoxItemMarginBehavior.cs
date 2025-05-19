using System.Windows;
using System.Windows.Controls;

namespace DevKit.Utils
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

                    if (index == 0)
                    {
                        item.Margin = new Thickness(0, 0, 3, 0); // 第一个去掉左间距
                    }
                    else if (index == count - 1)
                    {
                        item.Margin = new Thickness(3, 0, 0, 0); // 最后一个去掉右间距
                    }
                    else
                    {
                        item.Margin = new Thickness(3, 0, 3, 0);
                    }
                };
            }
        }
    }
}
