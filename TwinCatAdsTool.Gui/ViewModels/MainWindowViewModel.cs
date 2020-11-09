using System;
using System.Linq;
using System.Reflection;
using TwinCatAdsTool.Interfaces;
using TwinCatAdsTool.Interfaces.Commons;
using TwinCatAdsTool.Interfaces.Extensions;

namespace TwinCatAdsTool.Gui.ViewModels
{
    public class MainWindowViewModel : ViewModelBase
    {
        private readonly IViewModelFactory viewModelFactory;
        private string version;

        public MainWindowViewModel(IViewModelFactory viewModelFactory)
        {
            this.viewModelFactory = viewModelFactory;
        }

        public ConnectionCabViewModel ConnectionCabViewModel { get; set; }
        public TabsViewModel TabsViewModel { get; set; }

        public string Version
        {
            get => version;
            set
            {
                if (value == version) return;
                version = value;
                raisePropertyChanged();
            }
        }

        public override void Init()
        {
            Logger.Debug("Initialize main window view model");


            Version = $"v{Constants.Version}";

            ConnectionCabViewModel = viewModelFactory.CreateViewModel<ConnectionCabViewModel>();
            ConnectionCabViewModel.AddDisposableTo(Disposables);

            TabsViewModel = viewModelFactory.CreateViewModel<TabsViewModel>();
            TabsViewModel.AddDisposableTo(Disposables);
        }
    }
}