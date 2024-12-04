using System.Windows;
using System.Windows.Controls;
using DevKit.Cache;
using DevKit.Events;
using Prism.Events;

namespace DevKit.Dialogs
{
    public partial class ExCommandDialog : UserControl
    {
        private readonly IEventAggregator _eventAggregator;

        public ExCommandDialog(IEventAggregator eventAggregator)
        {
            InitializeComponent();
            _eventAggregator = eventAggregator;
        }
        
        private void DeleteMenuItem_Click(object sender, RoutedEventArgs e)
        {
            if (!(CommandListBox.SelectedItem is ExCommandCache cache))
            {
                return;
            }
        
            _eventAggregator.GetEvent<DeleteExCommandEvent>().Publish(cache);
        }
    }
}