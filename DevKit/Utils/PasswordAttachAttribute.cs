using System.Windows;
using System.Windows.Controls;

namespace DevKit.Utils
{
    /// <summary>
    /// PasswordBox密码值不是依赖属性，不可以作为绑定的目标与后台数据进行绑定
    /// </summary>
    public class PasswordAttachAttribute
    {
        public static readonly DependencyProperty PasswordProperty = DependencyProperty.RegisterAttached("Password",
            typeof(string), typeof(PasswordAttachAttribute),
            new FrameworkPropertyMetadata(string.Empty, OnPasswordPropertyChanged));

        public static readonly DependencyProperty AttachProperty = DependencyProperty.RegisterAttached("Attach",
            typeof(bool), typeof(PasswordAttachAttribute),
            new PropertyMetadata(false, OnAttachPropertyChanged));

        private static readonly DependencyProperty IsUpdatingProperty = DependencyProperty.RegisterAttached(
            "IsUpdating", typeof(bool), typeof(PasswordAttachAttribute));

        public static void SetAttach(DependencyObject dp, bool value)
        {
            dp.SetValue(AttachProperty, value);
        }

        public static bool GetAttach(DependencyObject dp)
        {
            return (bool)dp.GetValue(AttachProperty);
        }

        public static void SetPassword(DependencyObject dp, string value)
        {
            dp.SetValue(PasswordProperty, value);
        }

        public static string GetPassword(DependencyObject dp)
        {
            return (string)dp.GetValue(PasswordProperty);
        }

        private static void SetIsUpdating(DependencyObject dp, bool value)
        {
            dp.SetValue(IsUpdatingProperty, value);
        }

        private static bool GetIsUpdating(DependencyObject dp)
        {
            return (bool)dp.GetValue(IsUpdatingProperty);
        }

        private static void OnPasswordPropertyChanged(DependencyObject sender, DependencyPropertyChangedEventArgs e)
        {
            if (!(sender is PasswordBox box))
            {
                return;
            }

            box.PasswordChanged -= PasswordChanged;
            if (!GetIsUpdating(box))
            {
                box.Password = (string)e.NewValue;
            }

            box.PasswordChanged += PasswordChanged;
        }

        private static void OnAttachPropertyChanged(DependencyObject sender, DependencyPropertyChangedEventArgs e)
        {
            if (!(sender is PasswordBox box))
            {
                return;
            }

            if ((bool)e.OldValue)
            {
                box.PasswordChanged -= PasswordChanged;
            }
            else
            {
                box.PasswordChanged += PasswordChanged;
            }
        }

        private static void PasswordChanged(object sender, RoutedEventArgs e)
        {
            if (!(sender is PasswordBox box))
            {
                return;
            }

            SetIsUpdating(box, true);
            SetPassword(box, box.Password);
            SetIsUpdating(box, false);
        }
    }
}