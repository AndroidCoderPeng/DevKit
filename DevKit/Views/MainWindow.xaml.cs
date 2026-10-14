using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using DevKit.Models;
using DevKit.ViewModels;

namespace DevKit.Views
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow
    {
        public MainWindow()
        {
            InitializeComponent();
        }

        protected override void OnSourceInitialized(EventArgs e)
        {
            base.OnSourceInitialized(e);
            var workingArea = SystemParameters.WorkArea;
            Left = workingArea.Right - ActualWidth;
            Top = workingArea.Bottom - ActualHeight;
        }

        private void TopmostToggleButton_Checked(object sender, RoutedEventArgs e)
        {
            Topmost = true;
        }

        private void TopmostToggleButton_Unchecked(object sender, RoutedEventArgs e)
        {
            Topmost = false;
        }

        private void ToolsListBox_ListBoxItem_PreviewMouseDown(object sender, MouseButtonEventArgs e)
        {
            if (sender is ListBoxItem item && item.Content is MainMenu menu && DataContext is MainWindowViewModel vm)
            {
                vm.ToolClickedCommand.Execute(menu);
            }
        }
    }
}