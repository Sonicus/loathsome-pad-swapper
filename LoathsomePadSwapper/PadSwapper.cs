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
            if (Controller1 == null && Controller2 == null)
            {
                Debug.WriteLine("No physical controllers assigned, can't start virtual pad");
                return Task.FromException(new Exception("No physical controllers assigned, can't start virtual pad"));
            }
            Debug.WriteLine("Connecting to the virtual controller");
            _virtualController.Connect();
            _virtualControllerConnected = true;
            _virtualController.FeedbackReceived += VirtualRumbleEventHandler;
            ActiveController = Controller1 != null ? Controller1 : Controller2;
        }

        return Task.Run(() =>
        {
            var previousState = ActiveController!.GetState();
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
                if (ActiveController == null)
                {
                    Debug.WriteLine("ActiveController is null");
                    _virtualController.FeedbackReceived -= VirtualRumbleEventHandler;
                    _virtualController.Disconnect();
                    _virtualControllerConnected = false;
                    // TODO Handle this on the frontend side
                    return Task.FromException(new Exception("ActiveController is null"));
                }

                if (!ActiveController.IsConnected)
                {
                    if (ActiveController == Controller1 && Controller2 != null && Controller2.IsConnected)
                    {
                        Debug.WriteLine("Active Controller disconnected suddenly, switching active controller from Controller1 to Controller2");
                        ActiveController = Controller2;
                    }
                    else if (ActiveController == Controller2 && Controller1 != null && Controller1.IsConnected)
                    {
                        Debug.WriteLine("Active Controller disconnected suddenly, switching active controller from Controller2 to Controller1");
                        ActiveController = Controller1;
                    }
                    else
                    {
                        Debug.WriteLine("ActiveController disconnected suddenly and no other controller assigned, stopping virtual pad");
                        _virtualController.FeedbackReceived -= VirtualRumbleEventHandler;
                        _virtualController.Disconnect();
                        _virtualControllerConnected = false;
                        // TODO Handle this on the frontend side
                        return Task.FromException(new Exception("ActiveController is null"));
                    }
                }

                var state = ActiveController.GetState();
                if (previousState.PacketNumber != state.PacketNumber)
                {
                    PassStateToVirtualController(state.Gamepad);
                }
                previousState = state;
                Thread.Sleep(5);
            }
        });
    }

    public async Task ConnectToBaskDrinker(CancellationToken cancellationToken)
    {
        using var ws = new ClientWebSocket();

        var serializeOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = true
        };

        // TODO Error handling

        Debug.WriteLine("Connecting to Loathsome Bäsk Drinker...");
        await ws.ConnectAsync(new Uri("ws://localhost:10666/"), cancellationToken);

        Debug.WriteLine("Connected, sending hello...");
        await ws.SendAsync(
            Encoding.UTF8.GetBytes(JsonSerializer.Serialize(new BaskMessage.Hello(), options: serializeOptions)),
            WebSocketMessageType.Text,
            true,
            cancellationToken);
        Debug.WriteLine("Hello sent");

        Debug.WriteLine("Subscribing to nextPlayer topic...");
        await ws.SendAsync(
            Encoding.UTF8.GetBytes(JsonSerializer.Serialize(new BaskMessage.Subscribe(), options: serializeOptions)),
            WebSocketMessageType.Text,
            true,
            cancellationToken);
        Debug.WriteLine("Subscribed to Loathsome Bäsk Drinker events");

        var buffer = new byte[256];
        while (ws.State == WebSocketState.Open)
        {
            if (cancellationToken.IsCancellationRequested)
            {
                await ws.CloseAsync(WebSocketCloseStatus.NormalClosure, null, cancellationToken);
            }

            var result = await ws.ReceiveAsync(buffer, cancellationToken);
            if (result.MessageType == WebSocketMessageType.Close)
            {
                await ws.CloseAsync(WebSocketCloseStatus.NormalClosure, null, cancellationToken);
            }
            else
            {
                Debug.WriteLine($"Bäsk drinker event: {Encoding.UTF8.GetString(buffer, 0, result.Count)}");
                var eventMsg = JsonSerializer.Deserialize<BaskMessage.Event>(Encoding.UTF8.GetString(buffer, 0, result.Count), options: serializeOptions);
                if (eventMsg != null && eventMsg.MsgType == "nextPlayer")
                {
                    ToggleControllers();
                }
            }
        }
    }

    private void ToggleControllers()
    {

        Debug.WriteLine("Toggling controllers");
        if (ActiveController == null)
        {
            Debug.WriteLine("ActiveController is null, skipping controller toggle");
            return;
        }
        if (!ActiveController.IsConnected)
        {
            Debug.WriteLine("ActiveController is not connected, skipping controller toggle");
            return;
        }
        if (!_virtualControllerConnected)
        {
            Debug.WriteLine("VirtualController is not connected, skipping controller toggle");
            return;
        }

        if (ActiveController == Controller1)
        {
            if (Controller2 == null || !Controller2.IsConnected)
            {
                Debug.WriteLine("No other connected controllers, Controller 1 stays as the active controller");
            }
            else
            {
                // Set rumble to zero for the previous active controller
                SetRumbleToZero(ActiveController);
                ActiveController = Controller2;
                Debug.WriteLine("Controller 2 is the active controller");
            }
        }
        else
        {
            if (Controller1 == null ||!Controller1.IsConnected)
            {
                Debug.WriteLine("No other connected controllers, Controller 2 stays as the active controller");
            }
            else
            {
                // Set rumble to zero for the previous active controller
                SetRumbleToZero(ActiveController);
                ActiveController = Controller1;
                Debug.WriteLine("Controller 1 is the active controller");
            }
        }
    }

    private void PassStateToVirtualController(Gamepad gamepad)
    {
        if (_virtualControllerConnected == false || _virtualController == null)
        {
            return;
        }

        _virtualController.SetButtonState(Xbox360Button.A, gamepad.Buttons.HasFlag(GamepadButtonFlags.A));
        _virtualController.SetButtonState(Xbox360Button.B, gamepad.Buttons.HasFlag(GamepadButtonFlags.B));
        _virtualController.SetButtonState(Xbox360Button.X, gamepad.Buttons.HasFlag(GamepadButtonFlags.X));
        _virtualController.SetButtonState(Xbox360Button.Y, gamepad.Buttons.HasFlag(GamepadButtonFlags.Y));

        _virtualController.SetButtonState(Xbox360Button.Start, gamepad.Buttons.HasFlag(GamepadButtonFlags.Start));
        _virtualController.SetButtonState(Xbox360Button.Back, gamepad.Buttons.HasFlag(GamepadButtonFlags.Back));

        _virtualController.SetButtonState(Xbox360Button.LeftShoulder, gamepad.Buttons.HasFlag(GamepadButtonFlags.LeftShoulder));
        _virtualController.SetButtonState(Xbox360Button.RightShoulder, gamepad.Buttons.HasFlag(GamepadButtonFlags.RightShoulder));

        _virtualController.SetButtonState(Xbox360Button.LeftThumb, gamepad.Buttons.HasFlag(GamepadButtonFlags.LeftThumb));
        _virtualController.SetButtonState(Xbox360Button.RightThumb, gamepad.Buttons.HasFlag(GamepadButtonFlags.RightThumb));

        _virtualController.SetButtonState(Xbox360Button.Left, gamepad.Buttons.HasFlag(GamepadButtonFlags.DPadLeft));
        _virtualController.SetButtonState(Xbox360Button.Right, gamepad.Buttons.HasFlag(GamepadButtonFlags.DPadRight));
        _virtualController.SetButtonState(Xbox360Button.Up, gamepad.Buttons.HasFlag(GamepadButtonFlags.DPadUp));
        _virtualController.SetButtonState(Xbox360Button.Down, gamepad.Buttons.HasFlag(GamepadButtonFlags.DPadDown));

        _virtualController.SetAxisValue(Xbox360Axis.LeftThumbX, gamepad.LeftThumbX);
        _virtualController.SetAxisValue(Xbox360Axis.LeftThumbY, gamepad.LeftThumbY);
        _virtualController.SetAxisValue(Xbox360Axis.RightThumbX, gamepad.RightThumbX);
        _virtualController.SetAxisValue(Xbox360Axis.RightThumbY, gamepad.RightThumbY);

        _virtualController.SetSliderValue(Xbox360Slider.LeftTrigger, gamepad.LeftTrigger);
        _virtualController.SetSliderValue(Xbox360Slider.RightTrigger, gamepad.RightTrigger);
    }

    private void VirtualRumbleEventHandler(object sender, Xbox360FeedbackReceivedEventArgs e)
    {
        // Values from ViGem are bytes, SharpDX wants ushort
        var vibration = new Vibration { LeftMotorSpeed = (ushort)(e.LargeMotor * 257), RightMotorSpeed = (ushort)(e.SmallMotor * 257) };
        ActiveController?.SetVibration(vibration);
    }

    private void SetRumbleToZero(Controller gamepad)
    {
        gamepad.SetVibration(new Vibration { LeftMotorSpeed = 0, RightMotorSpeed = 0 });
    }
}