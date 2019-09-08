using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Reactive;
using System.Threading.Tasks;
using System.Windows;
using Microsoft.Win32;
using ReactiveUI;
using TwinCatAdsTool.Gui.Extensions;
using TwinCatAdsTool.Interfaces.Commons;

namespace TwinCatAdsTool.Gui.ViewModels
{
	public class MainWindowViewModel : ViewModelBase
	{
		private readonly IViewModelFactory viewModelFactory;
		private ViewModelBase selectedView;
		private string version;

		public MainWindowViewModel(IViewModelFactory viewModelFactory)
		{
			this.viewModelFactory = viewModelFactory;
		}

		public ReactiveCommand<Unit, Unit> OpenFileCommand { get; set; }


		public ObservableCollection<ViewModelBase> Views { get; } = new ObservableCollection<ViewModelBase>();

		public ViewModelBase SelectedView
		{
			get => selectedView;
			set
			{
				if (Equals(value, selectedView)) return;
				selectedView = value;
				raisePropertyChanged();
			}
		}

		public ReactiveCommand<object, Unit> DropCommand { get; set; }


		public override void Init()
		{
			Logger.Debug("Initialize main window view model");

			

			var version = System.Reflection.Assembly.GetExecutingAssembly().GetName().Version;
			Version = $"v.{version.Major}.{version.Minor}";

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

		

		
	}
}