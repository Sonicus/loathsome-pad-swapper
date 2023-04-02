
using SharpDX.XInput;
using System.Collections.ObjectModel;
using System.Diagnostics;

namespace LoathsomePadSwapper
{
    internal class PadSwapperViewModel : ViewModelBase
    {
        private bool _pad1ButtonEnabled;
        private bool _pad2ButtonEnabled;
        private Controller? _controller1;
        private Controller? _controller2;
        public bool Pad1ButtonEnabled { get => _pad1ButtonEnabled; private set => SetProperty(ref _pad1ButtonEnabled, value); }
        public bool Pad2ButtonEnabled { get => _pad2ButtonEnabled; private set => SetProperty(ref _pad2ButtonEnabled, value); }
        public Controller? Controller1 { get => _controller1; private set => SetProperty(ref _controller1, value); }
        public Controller? Controller2 { get => _controller2; private set => SetProperty(ref _controller2, value); }


        private readonly PadSwapper _padSwapper;
        private bool AssignmentPending;
        private CancellationTokenSource? _padAssignmentCancellationTokenSource;
        public ObservableCollection<Controller> Controllers { get; }

        public PadSwapperViewModel()
        {
            Pad1ButtonEnabled = true;
            Pad2ButtonEnabled = true;
            _padSwapper = new PadSwapper();

            Controllers = new ObservableCollection<Controller>();
            _padSwapper.Controllers.ForEach(c => Controllers.Add(c));
        }

        public void RefreshPads()
        {
            Debug.WriteLine("Refreshing pads");
            Controllers.Clear();
            _padSwapper.Controllers.ForEach(c => Controllers.Add(c));
        }

        public async Task AssignPad(int index)
        {
            if (AssignmentPending)
            {
                Debug.WriteLine("Cancelling pad assignment");
                _padAssignmentCancellationTokenSource.Cancel();
                _padAssignmentCancellationTokenSource.Dispose();
                return;
            }

            AssignmentPending = true;
            if (index == 1)
            {
                Pad2ButtonEnabled = false;
            }
            else
            {
                Pad1ButtonEnabled = false;
            }
            _padAssignmentCancellationTokenSource = new CancellationTokenSource();
            var cancellationToken = _padAssignmentCancellationTokenSource.Token;
            await _padSwapper.AssignController(index, cancellationToken);
            Controller1 = _padSwapper.Controller1;
            Controller2 = _padSwapper.Controller2;

            if (index == 1)
            {
                Pad2ButtonEnabled = true;
            }
            else
            {
                Pad1ButtonEnabled = true;
            }
            AssignmentPending = false;
        }
    }
}
