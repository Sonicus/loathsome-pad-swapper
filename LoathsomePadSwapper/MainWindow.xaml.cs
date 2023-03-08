using SharpDX.XInput;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;

namespace LoathsomePadSwapper
{
    public partial class MainWindow : Window
    {
        Controller? Pad1 { get; set; }
        Controller? Pad2 { get; set; }
        ObservableCollection<Controller> Controllers { get; set; } = new ObservableCollection<Controller>();
        bool AreButtonsEnabled { get; set; } = true;

        public MainWindow()
        {
            InitializeComponent();
            InitControllers();
            padList.ItemsSource = Controllers;
            pad1Button.DataContext = AreButtonsEnabled;
            pad2Button.DataContext = AreButtonsEnabled;
        }

        private void InitControllers()
        {
            for (int i = 0; i <= 3; i++)
            {
                Controllers.Add(new Controller((UserIndex)i));
            }
        }

        private void RefreshPads_Click(object sender, RoutedEventArgs e) => padList.Items.Refresh();

        private async void AssignControllerButton_Click(object sender, RoutedEventArgs e)
        {
            var button = sender as Button;
            var controllerIndex = Int32.Parse((string)button!.Tag);
            Debug.WriteLine($"Assigning controller {controllerIndex}");

            AreButtonsEnabled = false;
            button.Content = "Press A on a controller";
            await AssignController(controllerIndex);
            AreButtonsEnabled = true;

            button.Content = "OK";
            Debug.WriteLine($"Controller {controllerIndex} assigned");
        }

        Task AssignController(int index)
        {
            return Task.Run(() =>
            {
                while (true)
                {
                    foreach (var controller in Controllers)
                    {
                        if (controller.IsConnected && controller.GetState().Gamepad.Buttons == GamepadButtonFlags.A)
                        {
                            if (index == 1)
                            {
                                Pad1 = controller;
                            }
                            else
                            {
                                Pad2 = controller;
                            }
                            return;
                        }
                    }
                    Thread.Sleep(10);
                }
            });

        }


    }
}
