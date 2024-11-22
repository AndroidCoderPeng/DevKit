using DevKit.Utils;
using Prism.Services.Dialogs;

namespace DevKit.Views
{
    public partial class ExCommandWindow : IDialogWindow
    {
        public IDialogResult Result { get; set; }

        public ExCommandWindow()
        {
            InitializeComponent();
            Left = RuntimeCache.X;
            Top = RuntimeCache.Y;
        }
    }
}