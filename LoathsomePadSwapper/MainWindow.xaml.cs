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

        private void AssignControllerButton_Click(object sender, RoutedEventArgs e)
        {
            var button = sender as Button;
            var controllerIndex = Int32.Parse((string)button!.Tag);
            Debug.WriteLine($"Assigning controller {controllerIndex}");

            if( controllerIndex == 1 ) {
                ViewModel.Pad2ButtonEnabled = !ViewModel.Pad2ButtonEnabled;
            } else
            {
                ViewModel.Pad1ButtonEnabled = !ViewModel.Pad1ButtonEnabled;
            }
        }
    }
}
