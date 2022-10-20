# Loathsome Pad Swapper

Creates a virtual gamepad and swaps the control of it between real gamepads. Hardcoded to work with [Loathsome Bask Drinker](https://github.com/SirDifferential/baskdrinker).

Uses [ViGEm Client .NET SDK](https://github.com/ViGEm/ViGEm.NET) to interact with the [ViGEm Bus Driver](https://github.com/ViGEm/ViGEmBus). Physical controllers are read using [SharpDX.XInput](https://github.com/sharpdx/SharpDX).

## Requirements

- Windows
- [ViGEm Bus Driver](https://github.com/ViGEm/ViGEmBus) (Developed on version 1.21.442)
- [.NET 6](https://dotnet.microsoft.com/en-us/download/dotnet/6.0) (Runtime for the pre-built application, SDK for building)

## How to run

`dotnet run --project LoathsomePadSwapper`

Or run the published executable.

`Space` toggles the active controller. `Esc` exits. For timers use [Loathsome Bask Drinker](https://github.com/SirDifferential/baskdrinker).

## How to use

1. Start Loathsome Bask Drinker and its server
2. Turn on two controllers (tested with Xbox One and Series X pads)
3. Start Loathsome Pad Swapper and assign the controllers.
4. Play a game and assign the virtual controller as the primary controller.
