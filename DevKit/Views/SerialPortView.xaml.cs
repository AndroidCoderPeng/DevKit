﻿using System.Windows;
using System.Windows.Controls;
using DevKit.Models;

namespace DevKit.Views
{
    public partial class SerialPortView : UserControl
    {
        public SerialPortView()
        {
            InitializeComponent();
        }

        private void MenuItem_Click(object sender, RoutedEventArgs e)
        {
            if (!(MessageListBox.SelectedItem is MessageModel message)) return;
            var content = message.Content;
            Clipboard.SetText(content);
        }
    }
}