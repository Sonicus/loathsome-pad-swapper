using LoathsomePadSwapper;
using Nefarius.ViGEm.Client;
using Nefarius.ViGEm.Client.Targets;
using Nefarius.ViGEm.Client.Targets.Xbox360;
using SharpDX.XInput;
using System.Diagnostics;
using System.Net.WebSockets;
using System.Text;
using System.Text.Json;

internal class PadSwapper
{
    public List<Controller> Controllers { get; }
    public Controller? Controller1 { get; private set; }
    public Controller? Controller2 { get; private set; }
    public Controller? ActiveController { get; private set; }

    private ViGEmClient _vigemClient;
    private IXbox360Controller _virtualController;
    private bool _virtualControllerConnected;

    public PadSwapper()
    {
        Controllers = new List<Controller> { new Controller(UserIndex.One), new Controller(UserIndex.Two), new Controller(UserIndex.Three), new Controller(UserIndex.Four) };
        _vigemClient = new ViGEmClient();
        _virtualController = _vigemClient.CreateXbox360Controller();
    }

    public Task AssignController(int index, CancellationToken cancellationToken)
    {
        Debug.WriteLine($"Assigning controller {index}");

        return Task.Run(() =>
        {
            while (true)
            {
                if (cancellationToken.IsCancellationRequested)
                {
                    Debug.WriteLine("Controller assignment cancelled");
                    return;
                }
                foreach (var controller in Controllers)
                {
                    if (controller.IsConnected && controller.GetState().Gamepad.Buttons == GamepadButtonFlags.A)
                    {
                        if (index == 1)
                        {
                            Controller1 = controller;
                            Debug.WriteLine($"Assigned controller {index} with pad {controller.UserIndex}");
                            if (Controller2 != null && Controller2.UserIndex == Controller1.UserIndex)
                            {
                                Controller2 = null;
                                Debug.WriteLine($"Unassigned controller 2 due to same pad being assigned to controller 1");
                            }
                        }
                        else
                        {
                            Controller2 = controller;
                            Debug.WriteLine($"Assigned controller {index} with pad {controller.UserIndex}");
                            if (Controller1 != null && Controller1.UserIndex == Controller2.UserIndex)
                            {
                                Controller1 = null;
                                Debug.WriteLine($"Unassigned controller 1 due to same pad being assigned to controller 2");
                            }
                        }

                        return;
                    }
                }
                Thread.Sleep(10);
            }
        });
    }

    public Task RunVirtualPad(CancellationToken cancellationToken)
    {
        if (_virtualControllerConnected == false)
        {
            Debug.WriteLine("Connecting to the virtual controller");
            _virtualController.Connect();
            _virtualControllerConnected = true;
            _virtualController.FeedbackReceived += VirtualRumbleEventHandler;
        }

        return Task.Run(() =>
        {
            while (true)
            {
                if (cancellationToken.IsCancellationRequested)
                {
                    Debug.WriteLine("Virtual pad cancellation requested, disconnecting...");
                    _virtualController.FeedbackReceived -= VirtualRumbleEventHandler;
                    _virtualController.Disconnect();
                    _virtualControllerConnected = false;
                    return Task.CompletedTask;
                }
                Thread.Sleep(10);
            }
        });
    }

    private void VirtualRumbleEventHandler(object sender, Xbox360FeedbackReceivedEventArgs e)
    {
        // Values from ViGem are bytes, SharpDX wants ushort
        var vibration = new Vibration { LeftMotorSpeed = (ushort)(e.LargeMotor * 257), RightMotorSpeed = (ushort)(e.SmallMotor * 257) };
        ActiveController?.SetVibration(vibration);
    }

    private static void Maini(string[] args)
    {
        Console.WriteLine("Starting Loathsome Pad Swapper...");

        var controllers = new[] { new Controller(UserIndex.One), new Controller(UserIndex.Two), new Controller(UserIndex.Three), new Controller(UserIndex.Four) }.Where(controller => controller.IsConnected);
        Console.WriteLine($"Found {controllers.Count()} connected controllers");

        #region Initialize physical controllers
        Console.WriteLine("Press A on the first controller");
        var controller1 = AssignController(controllers, null);

        Console.WriteLine("Press A on the second controller");
        var controller2 = AssignController(controllers, controller1.UserIndex);

        Console.WriteLine("Controllers assigned");
        #endregion

        #region Initialize virtual controller
        Console.WriteLine("Initializing virtual controller...");

        var client = new ViGEmClient();
        var virtualController = client.CreateXbox360Controller();
        virtualController.Connect();

        Console.WriteLine("Virtual controller initialized");

        var activeController = controller1;
        Console.WriteLine($"{(ushort)activeController.UserIndex + 1} is the active controller");
        #endregion

        #region Connect to Bask Drinker
        Task.Run(ConnectToBaskDrinker);
        #endregion

        #region Update virtual controller state
        var previousState = activeController.GetState();
        while (activeController.IsConnected)
        {
            if (IsKeyPressed(ConsoleKey.Escape))
            {
                break;
            }
            if (IsKeyPressed(ConsoleKey.Spacebar))
            {
                ToggleControllers();
            }

            var state = activeController.GetState();
            if (previousState.PacketNumber != state.PacketNumber)
            {
                PassStateToVirtualController(state.Gamepad);
            }
            previousState = state;
            Thread.Sleep(1);

        }
        #endregion

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

        void ToggleControllers()
        {
            activeController = activeController.UserIndex == controller1.UserIndex ? controller2 : controller1;
            Console.WriteLine($"{(ushort)activeController.UserIndex + 1} is the active controller");
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

        async Task ConnectToBaskDrinker()
        {
            using var ws = new ClientWebSocket();

            var serializeOptions = new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                WriteIndented = true
            };

            Console.WriteLine("Connecting to Loathsome Bäsk Drinker...");
            await ws.ConnectAsync(new Uri("ws://localhost:10666/"), CancellationToken.None);

            Console.WriteLine("Connected, sending hello...");
            await ws.SendAsync(
                Encoding.UTF8.GetBytes(JsonSerializer.Serialize(new BaskMessage.Hello(), options: serializeOptions)),
                WebSocketMessageType.Text,
                true,
                CancellationToken.None);
            Console.WriteLine("Hello sent");

            Console.WriteLine("Subscribing to nextPlayer topic...");
            await ws.SendAsync(
                Encoding.UTF8.GetBytes(JsonSerializer.Serialize(new BaskMessage.Subscribe(), options: serializeOptions)),
                WebSocketMessageType.Text,
                true,
                CancellationToken.None);
            Console.WriteLine("Subscribed to Loathsome Bäsk Drinker events");

            var buffer = new byte[256];
            while (ws.State == WebSocketState.Open)
            {
                var result = await ws.ReceiveAsync(buffer, CancellationToken.None);
                if (result.MessageType == WebSocketMessageType.Close)
                {
                    await ws.CloseAsync(WebSocketCloseStatus.NormalClosure, null, CancellationToken.None);
                }
                else
                {
                    Console.WriteLine($"Bäsk drinker event: {Encoding.UTF8.GetString(buffer, 0, result.Count)}");
                    var eventMsg = JsonSerializer.Deserialize<BaskMessage.Event>(Encoding.UTF8.GetString(buffer, 0, result.Count), options: serializeOptions);
                    if (eventMsg != null && eventMsg.MsgType == "nextPlayer")
                    {
                        ToggleControllers();
                    }
                }
            }
        }
    }
}