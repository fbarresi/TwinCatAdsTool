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

		public override void Init()
		{
			Logger.Debug("Initialize main window view model");

			
			var version = System.Reflection.Assembly.GetExecutingAssembly().GetName().Version;
			Version = $"v.{version.Major}.{version.Minor}";

            ConnectionCabViewModel = viewModelFactory.CreateViewModel<ConnectionCabViewModel>();
            ConnectionCabViewModel.AddDisposableTo(Disposables);

            TabsViewModel = viewModelFactory.CreateViewModel<TabsViewModel>();
            TabsViewModel.AddDisposableTo(Disposables);


        }

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

        public ConnectionCabViewModel ConnectionCabViewModel { get; set; }
        public TabsViewModel TabsViewModel { get; set; }
		
	}
}