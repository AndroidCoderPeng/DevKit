using System.Windows;
using System.Windows.Controls;

namespace DevKit.Views
{
    public partial class GenerateIconView : UserControl
    {
        public GenerateIconView()
        {
            InitializeComponent();
            TypeComboBox.DropDownClosed += delegate
            {
                var text = TypeComboBox.Text;
                switch (text)
                {
                    case "Windows":
                        WindowsIconListBox.Visibility = Visibility.Visible;
                        AndroidDrawableListBox.Visibility = Visibility.Collapsed;
                        IPhoneImageListBox.Visibility = Visibility.Collapsed;

                        IcoRadioButton.IsEnabled = true;
                        IcoRadioButton.IsChecked = true;
                        break;
                    case "Android":
                        WindowsIconListBox.Visibility = Visibility.Collapsed;
                        AndroidDrawableListBox.Visibility = Visibility.Visible;
                        IPhoneImageListBox.Visibility = Visibility.Collapsed;

                        IcoRadioButton.IsEnabled = false;
                        PngRadioButton.IsChecked = true;
                        break;
                    default:
                        WindowsIconListBox.Visibility = Visibility.Collapsed;
                        AndroidDrawableListBox.Visibility = Visibility.Collapsed;
                        IPhoneImageListBox.Visibility = Visibility.Visible;

                        IcoRadioButton.IsEnabled = false;
                        PngRadioButton.IsChecked = true;
                        break;
                }
            };
        }
    }
}