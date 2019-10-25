using System;
using System.Collections.ObjectModel;
using System.Reactive.Linq;
using TwinCAT.TypeSystem;
using TwinCatAdsTool.Interfaces.Commons;
using TwinCatAdsTool.Interfaces.Extensions;
using TwinCatAdsTool.Interfaces.Services;

namespace TwinCatAdsTool.Gui.ViewModels
{
    public class ObserverViewModel : ViewModelBase
    {
        private readonly IViewModelFactory viewModelFactory;
        private readonly ISelectionService<ISymbol> symbolSelection;

        public ObserverViewModel(IViewModelFactory viewModelFactory, ISelectionService<ISymbol> symbolSelection)
        {
            this.viewModelFactory = viewModelFactory;
            this.symbolSelection = symbolSelection;
        }

        public ObservableCollection<SymbolObservationViewModel> ViewModels { get; set; } = new ObservableCollection<SymbolObservationViewModel>();

        public override void Init()
        {
            ViewModels.Clear();
            symbolSelection.Elements
                .ObserveOnDispatcher()
                .Do(symbol =>ViewModels.Add(viewModelFactory.CreateViewModel<ISymbol,SymbolObservationViewModel>(symbol)))

                .Subscribe()
                .AddDisposableTo(Disposables);
        }
    }
}