using System;
using System.Windows.Controls;
using System.Windows.Input;
using Microsoft.Xaml.Behaviors;

namespace DevKit.Behaviors
{
    public class NumericRangeBehavior : Behavior<TextBox>
    {
        public int MinValue { get; set; } = 0;
        public int MaxValue { get; set; } = 255;

        protected override void OnAttached()
        {
            base.OnAttached();
            AssociatedObject.PreviewTextInput += OnPreviewTextInput;
            AssociatedObject.PreviewKeyDown += OnPreviewKeyDown;
            AssociatedObject.TextChanged += OnTextChanged;
        }

        protected override void OnDetaching()
        {
            base.OnDetaching();
            AssociatedObject.PreviewTextInput -= OnPreviewTextInput;
            AssociatedObject.PreviewKeyDown -= OnPreviewKeyDown;
            AssociatedObject.TextChanged -= OnTextChanged;
        }

        private void OnPreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            // 只允许输入数字
            e.Handled = !IsTextAllowed(e.Text);
        }

        private void OnPreviewKeyDown(object sender, KeyEventArgs e)
        {
            // 允许退格键和删除键
            if (e.Key == Key.Back || e.Key == Key.Delete)
            {
                e.Handled = false;
            }
        }
        
        private void OnTextChanged(object sender, TextChangedEventArgs e)
        {
            if (!(sender is TextBox textBox)) return;
            if (int.TryParse(textBox.Text, out var value))
            {
                // 限制范围
                if (value >= MinValue && value <= MaxValue) return;
                textBox.Text = Math.Max(MinValue, Math.Min(MaxValue, value)).ToString();
                textBox.CaretIndex = textBox.Text.Length;
            }
            else if (!string.IsNullOrEmpty(textBox.Text))
            {
                // 如果不是有效数字，清空内容
                textBox.Text = "";
            }
        }
        
        private static bool IsTextAllowed(string text)
        {
            return int.TryParse(text, out _);
        }
    }
}