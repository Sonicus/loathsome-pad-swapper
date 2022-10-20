using Nefarius.ViGEm.Client;
using Nefarius.ViGEm.Client.Targets.Xbox360;
using SharpDX.XInput;

internal class Program
{
    private static void Main(string[] args)
    {
        Console.WriteLine("Starting Loathsome Pad Swapper...");

        // Initialize XInput
        var controllers = new[] { new Controller(UserIndex.One), new Controller(UserIndex.Two), new Controller(UserIndex.Three), new Controller(UserIndex.Four) }.Where(controller => controller.IsConnected);
        Console.WriteLine($"Found {controllers.Count()} connected controllers");

        // Assign controllers
        Console.WriteLine("Press A on the first controller");
        var controller1 = AssignController(controllers, null);

        Console.WriteLine("Press A on the second controller");
        var controller2 = AssignController(controllers, controller1.UserIndex);

        Console.WriteLine("Controllers assigned");

        // Initialize virtual controller
        Console.WriteLine("Initializing virtual controller...");

        var client = new ViGEmClient();
        var virtualController = client.CreateXbox360Controller();
        virtualController.Connect();

        Console.WriteLine("Virtual controller initialized");

        var activeController = controller1;
        Console.WriteLine($"{(ushort)activeController.UserIndex + 1} is the active controller");

        var previousState = activeController.GetState();
        while (activeController.IsConnected)
        {
            if (IsKeyPressed(ConsoleKey.Escape))
            {
                break;
            }
            if (IsKeyPressed(ConsoleKey.Spacebar))
            {
                activeController = activeController.UserIndex == controller1.UserIndex ? controller2 : controller1;
                Console.WriteLine($"{(ushort)activeController.UserIndex + 1} is the active controller");
            }

            var state = activeController.GetState();
            if (previousState.PacketNumber != state.PacketNumber)
            {
                PassStateToVirtualController(state.Gamepad);
            }
            previousState = state;
        }

        void PassStateToVirtualController(Gamepad gamepad)
        {
            virtualController.SetButtonState(Xbox360Button.A, gamepad.Buttons.HasFlag(GamepadButtonFlags.A));
            virtualController.SetButtonState(Xbox360Button.B, gamepad.Buttons.HasFlag(GamepadButtonFlags.B));
            virtualController.SetButtonState(Xbox360Button.X, gamepad.Buttons.HasFlag(GamepadButtonFlags.X));
            virtualController.SetButtonState(Xbox360Button.Y, gamepad.Buttons.HasFlag(GamepadButtonFlags.Y));

            virtualController.SetButtonState(Xbox360Button.Start, gamepad.Buttons.HasFlag(GamepadButtonFlags.Start));
            virtualController.SetButtonState(Xbox360Button.Back, gamepad.Buttons.HasFlag(GamepadButtonFlags.Back));

            virtualController.SetButtonState(Xbox360Button.LeftShoulder, gamepad.Buttons.HasFlag(GamepadButtonFlags.LeftShoulder));
            virtualController.SetButtonState(Xbox360Button.RightShoulder, gamepad.Buttons.HasFlag(GamepadButtonFlags.RightShoulder));

            virtualController.SetButtonState(Xbox360Button.LeftThumb, gamepad.Buttons.HasFlag(GamepadButtonFlags.LeftThumb));
            virtualController.SetButtonState(Xbox360Button.RightThumb, gamepad.Buttons.HasFlag(GamepadButtonFlags.RightThumb));

            virtualController.SetButtonState(Xbox360Button.Left, gamepad.Buttons.HasFlag(GamepadButtonFlags.DPadLeft));
            virtualController.SetButtonState(Xbox360Button.Right, gamepad.Buttons.HasFlag(GamepadButtonFlags.DPadRight));
            virtualController.SetButtonState(Xbox360Button.Up, gamepad.Buttons.HasFlag(GamepadButtonFlags.DPadUp));
            virtualController.SetButtonState(Xbox360Button.Down, gamepad.Buttons.HasFlag(GamepadButtonFlags.DPadDown));

            virtualController.SetAxisValue(Xbox360Axis.LeftThumbX, gamepad.LeftThumbX);
            virtualController.SetAxisValue(Xbox360Axis.LeftThumbY, gamepad.LeftThumbY);
            virtualController.SetAxisValue(Xbox360Axis.RightThumbX, gamepad.RightThumbX);
            virtualController.SetAxisValue(Xbox360Axis.RightThumbY, gamepad.RightThumbY);

            virtualController.SetSliderValue(Xbox360Slider.LeftTrigger, gamepad.LeftTrigger);
            virtualController.SetSliderValue(Xbox360Slider.RightTrigger, gamepad.RightTrigger);
        }


        bool IsKeyPressed(ConsoleKey key)
        {
            return Console.KeyAvailable && Console.ReadKey(true).Key == key;
        }

        Controller AssignController(IEnumerable<Controller> controllers, UserIndex? takenIndex)
        {
            while (true)
            {
                foreach (var controller in controllers)
                {
                    if (controller.IsConnected && controller.UserIndex != takenIndex && controller.GetState().Gamepad.Buttons == GamepadButtonFlags.A)
                    {
                        return controller;
                    }
                }
                Thread.Sleep(10);
            }
        }
    }
}