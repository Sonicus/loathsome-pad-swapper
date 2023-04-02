
namespace LoathsomePadSwapper
{
    internal class PadSwapperViewModel : ViewModelBase
    {
        private bool _pad1ButtonEnabled;
        private bool _pad2ButtonEnabled;

        public bool Pad1ButtonEnabled { get => _pad1ButtonEnabled; set => SetProperty(ref _pad1ButtonEnabled, value); }
        public bool Pad2ButtonEnabled { get => _pad2ButtonEnabled; set => SetProperty(ref _pad2ButtonEnabled, value); }

        public PadSwapperViewModel()
        {
            Pad1ButtonEnabled = true;
            Pad2ButtonEnabled = true;
        }
    }
}
