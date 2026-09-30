using System;
using System.Threading.Tasks;
using System.Windows;
using DevKit.Cache;
using DevKit.Utils;
using Prism.Commands;
using Prism.Mvvm;
using Prism.Services.Dialogs;
using Color = System.Windows.Media.Color;

namespace DevKit.ViewModels
{
    public class ColorResourceViewModel : BindableBase, IDialogAware
    {
        public string Title => "颜色处理";

        public event Action<IDialogResult> RequestClose
        {
            add { }
            remove { }
        }

        public bool CanCloseDialog()
        {
            return true;
        }

        public void OnDialogClosed()
        {
        }

        public void OnDialogOpened(IDialogParameters parameters)
        {
        }

        #region VM

        

        #endregion

        #region DelegateCommand

        public DelegateCommand<string> AlphaColorTextChangedCommand { set; get; }
        public DelegateCommand<string> RedColorTextChangedCommand { set; get; }
        public DelegateCommand<string> GreenColorTextChangedCommand { set; get; }
        public DelegateCommand<string> BlueColorTextChangedCommand { set; get; }
        public DelegateCommand<string> ColorHexTextChangedCommand { set; get; }

        public DelegateCommand HexCheckBoxCheckedCommand { set; get; }
        public DelegateCommand HexCheckBoxUncheckedCommand { set; get; }

        public DelegateCommand AlphaCheckBoxCheckedCommand { set; get; }
        public DelegateCommand AlphaCheckBoxUncheckedCommand { set; get; }

        public DelegateCommand CopyColorHexValueCommand { set; get; }
        public DelegateCommand<ColorResourceCache> ColorItemClickedCommand { set; get; }

        #endregion

        private byte _alpha = 255;
        private byte _red;
        private byte _green;
        private byte _blue;

        public ColorResourceViewModel()
        {
            Task.Run(async () => await LoadColorResourcesAsync());

            var color = Color.FromRgb(0, 0, 0);
            // ColorViewBrush = new SolidColorBrush(color);
        }

        private async Task LoadColorResourcesAsync()
        {
            try
            {
                using (var dataBase = new DataBaseConnection())
                {
                    var colorResCaches = await Task.Run(() => dataBase.Table<ColorResourceCache>().ToList());
                    await Application.Current.Dispatcher.InvokeAsync(() =>
                    {
                        // ColorResources = colorResCaches.ToObservableCollection();
                    });
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
        }
    }
}