using TwinCatAdsTool.Interfaces.Commons;
using TwinCatAdsTool.Interfaces.Extensions;

namespace TwinCatAdsTool.Gui.ViewModels
{
    public class TabsViewModel : ViewModelBase
    {
        private readonly IViewModelFactory viewModelFactory;

        public TabsViewModel(IViewModelFactory viewModelFactory)
        {
            this.viewModelFactory = viewModelFactory;
        }

        public override void Init()
        {
            BackupViewModel = viewModelFactory.CreateViewModel<BackupViewModel>();
            BackupViewModel.AddDisposableTo(Disposables);

            CompareViewModel = viewModelFactory.CreateViewModel<CompareViewModel>();
            CompareViewModel.AddDisposableTo(Disposables);
        }

        public BackupViewModel BackupViewModel { get; set; }
        public CompareViewModel CompareViewModel { get; set; }
    }
}