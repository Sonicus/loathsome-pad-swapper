using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;

namespace LoathsomePadSwapper
{
    public partial class MainWindow : Window
    {
        PadSwapperViewModel ViewModel { get; }

        public MainWindow()
        {
            ViewModel = new PadSwapperViewModel();
            DataContext = ViewModel;
            InitializeComponent();
        }

        private async void AssignControllerButton_Click(object sender, RoutedEventArgs e)
        {
            var button = sender as Button;
            var controllerIndex = int.Parse((string)button!.Tag);

            await ViewModel.AssignPad(controllerIndex);
        }

        private void RefreshPadsButton_Click(object sender, RoutedEventArgs e)
        {
            ViewModel.RefreshPads();
        }
    }
}
