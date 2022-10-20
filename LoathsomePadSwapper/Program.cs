using Nefarius.ViGEm.Client;
using Nefarius.ViGEm.Client.Targets.Xbox360;
using SharpDX.XInput;

Console.WriteLine("Starting Loathsome Pad Swapper...");

Controller? _controller1 = null;
Controller? _controller2 = null;

// Initialize XInput
var controllers = new[] { new Controller(UserIndex.One), new Controller(UserIndex.Two), new Controller(UserIndex.Three), new Controller(UserIndex.Four) }.Where(controller => controller.IsConnected);
Console.WriteLine($"Found {controllers.Count()} connected controllers");

// Assign first controller
Console.WriteLine("Press A on the first controller");

while (true)
{
    foreach (var controller in controllers)
    {
        if (controller.IsConnected && controller.GetState().Gamepad.Buttons == GamepadButtonFlags.A)
        {
            _controller1 = controller;
            break;
        }
    }
    if (_controller1 != null) break;
    Thread.Sleep(10);
}

// Assign second controller
Console.WriteLine("Press A on the second controller");

while (true)
{
    foreach (var controller in controllers)
    {
        if (controller.IsConnected && controller.UserIndex != _controller1.UserIndex && controller.GetState().Gamepad.Buttons == GamepadButtonFlags.A)
        {
            _controller2 = controller;
            break;
        }
    }
    if (_controller2 != null) break;
    Thread.Sleep(10);
}

Console.WriteLine("Controllers assigned");
Console.WriteLine("Initializing virtual controller...");

var client = new ViGEmClient();
var virtualController = client.CreateXbox360Controller();
virtualController.Connect();

Console.WriteLine("Virtual controller initialized");

var activeController = _controller1;
Console.WriteLine($"{(ushort)activeController.UserIndex + 1} is the active controller");
var previousState = _controller1.GetState();
while (activeController.IsConnected)
{
    if (IsKeyPressed(ConsoleKey.Escape))
    {
        break;
    }
    if (IsKeyPressed(ConsoleKey.Spacebar))
    {
        activeController = activeController.UserIndex == _controller1.UserIndex ? _controller2 : _controller1;
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