using System.Windows.Controls;
using Prism.Events;

namespace DevKit.Views
{
    public partial class AndroidDebugBridgeView : UserControl
    {
        public AndroidDebugBridgeView(IEventAggregator eventAggregator)
        {
            InitializeComponent();
            // eventAggregator.GetEvent<ResizeGridWidthEvent>().Subscribe(delegate
            // {
            //     var rightWidth = RightPanelColumn.Width;
            //     if (rightWidth.Value > 0)
            //     {
            //         // 已展开，隐藏右侧面板
            //         RightPanel.Visibility = Visibility.Collapsed;
            //         LeftPanelColumn.Width = new GridLength(1, GridUnitType.Star);
            //         RightPanelColumn.Width = new GridLength(0);
            //     }
            //     else
            //     {
            //         // 未展开，显示右侧面板
            //         RightPanel.Visibility = Visibility.Visible;
            //         LeftPanelColumn.Width = new GridLength(365);
            //         RightPanelColumn.Width = new GridLength(250);
            //     }
            // });
        }
    }
}