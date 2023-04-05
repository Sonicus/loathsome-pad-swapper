using System.Windows;

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

        private void RefreshPadsButton_Click(object sender, RoutedEventArgs e)
        {
            ViewModel.RefreshPads();
        }
    }
}
