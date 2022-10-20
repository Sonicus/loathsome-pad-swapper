using Nefarius.ViGEm.Client;
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

var previousState1 = _controller1.GetState();
while (_controller1.IsConnected)
{
    if (IsKeyPressed(ConsoleKey.Escape))
    {
        break;
    }
    var state1 = _controller1.GetState();
    if (previousState1.PacketNumber != state1.PacketNumber)
        Console.WriteLine(state1.Gamepad);
    previousState1 = state1;
}


bool IsKeyPressed(ConsoleKey key)
{
    return Console.KeyAvailable && Console.ReadKey(true).Key == key;
}