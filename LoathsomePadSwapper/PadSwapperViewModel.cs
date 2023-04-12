
using CommunityToolkit.Mvvm.Input;
using SharpDX.XInput;
using System;
using System.Collections.ObjectModel;
using System.Diagnostics;

namespace LoathsomePadSwapper
{
    internal class PadSwapperViewModel : ViewModelBase
    {
        private readonly PadSwapper _padSwapper;

        public ObservableCollection<Controller> Controllers { get; }
        private int? _controllerIndexBeingAssigned;

        private Controller? _controller1;
        public Controller? Controller1 { get => _controller1; private set => SetProperty(ref _controller1, value); }
        private Controller? _controller2;
        public Controller? Controller2 { get => _controller2; private set => SetProperty(ref _controller2, value); }
        private bool _isSwapperRunning;
        public bool IsSwapperRunning { get => _isSwapperRunning; private set => SetProperty(ref _isSwapperRunning, value); }


        public IRelayCommand RefreshPadsCommand { get; }
        public IRelayCommand RunVirtualPadCommand { get; }
        public IAsyncRelayCommand AssignController1Command { get; }
        public IAsyncRelayCommand AssignController2Command { get; }

        private CancellationTokenSource? _padAssignmentCancellationTokenSource;
        private CancellationTokenSource? _swapperCancellationTokenSource;

        public PadSwapperViewModel()
        {
            _padSwapper = new PadSwapper();

            Controllers = new ObservableCollection<Controller>();
            _padSwapper.Controllers.ForEach(c => Controllers.Add(c));

            RefreshPadsCommand = new RelayCommand(RefreshPads);
            RunVirtualPadCommand = new RelayCommand(StartStopSwapper, () => _padSwapper.Controller1 != null || _padSwapper.Controller2 != null);
            AssignController1Command = new AsyncRelayCommand(AssignController1, () => _controllerIndexBeingAssigned == null || _controllerIndexBeingAssigned == 1, AsyncRelayCommandOptions.AllowConcurrentExecutions);
            AssignController2Command = new AsyncRelayCommand(AssignController2, () => _controllerIndexBeingAssigned == null || _controllerIndexBeingAssigned == 2, AsyncRelayCommandOptions.AllowConcurrentExecutions);
        }

        public void RefreshPads()
        {
            Debug.WriteLine("Refreshing pads");
            Controllers.Clear();
            _padSwapper.Controllers.ForEach(c => Controllers.Add(c));
        }

        private async Task AssignController1()
        {
            await AssignController(1);
        }

        private async Task AssignController2()
        {
            await AssignController(2);
        }

        private async Task AssignController(int index)
        {
            Debug.WriteLine($"Index for AssignPad command: {index}");

            if (_controllerIndexBeingAssigned != null)
            {
                Debug.WriteLine("Cancelling pad assignment");
                _padAssignmentCancellationTokenSource!.Cancel();
                _padAssignmentCancellationTokenSource.Dispose();
                return;
            }

            _controllerIndexBeingAssigned = index;
            AssignController1Command.NotifyCanExecuteChanged();
            AssignController2Command.NotifyCanExecuteChanged();

            _padAssignmentCancellationTokenSource = new CancellationTokenSource();
            var cancellationToken = _padAssignmentCancellationTokenSource.Token;
            await _padSwapper.AssignController(index, cancellationToken);
            Controller1 = _padSwapper.Controller1;
            Controller2 = _padSwapper.Controller2;

            _controllerIndexBeingAssigned = null;
            AssignController1Command.NotifyCanExecuteChanged();
            AssignController2Command.NotifyCanExecuteChanged();
            RunVirtualPadCommand.NotifyCanExecuteChanged();
        }

        private void StartStopSwapper()
        {
            if (IsSwapperRunning)
            {
                Debug.WriteLine("Stopping Pad Swapper");
                _swapperCancellationTokenSource!.Cancel();
                _swapperCancellationTokenSource.Dispose();
                IsSwapperRunning = false;
                return;
            }

            Debug.WriteLine("Starting Pad Swapper");
            _swapperCancellationTokenSource = new CancellationTokenSource();
            var cancellationToken = _swapperCancellationTokenSource.Token;
            _padSwapper.RunVirtualPad(cancellationToken);
            IsSwapperRunning = true;
        }
    }
}
