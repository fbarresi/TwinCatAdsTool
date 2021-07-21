using System;
using System.Collections.ObjectModel;
using System.Reactive.Linq;
using System.Linq;
using System.Windows;
using TwinCAT.PlcOpen;
using TwinCAT.TypeSystem;
using TwinCatAdsTool.Gui.Properties;
using TwinCatAdsTool.Interfaces.Commons;
using TwinCatAdsTool.Interfaces.Extensions;
using TwinCatAdsTool.Interfaces.Services;

namespace TwinCatAdsTool.Gui.ViewModels
{
    public class ObserverViewModel : ViewModelBase
    {
        private readonly ISelectionService<ISymbol> symbolSelection;
        private readonly IViewModelFactory viewModelFactory;

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
                .Do(CreateViewModelOrShowMessage)
                .Subscribe()
                .AddDisposableTo(Disposables);
        }

        private void CreateViewModelOrShowMessage(ISymbol symbol)
        {
            if (ViewModels.All(viewModel => viewModel.Model != symbol))
            {
                switch (symbol.TypeName)
                {
                    case "BOOL":
                        ViewModels.Add(viewModelFactory.CreateViewModel<ISymbol, SymbolObservationViewModel<bool>>(symbol));
                        break;
                    case "BYTE":
                        ViewModels.Add(viewModelFactory.CreateViewModel<ISymbol, SymbolObservationViewModel<byte>>(symbol));
                        break;
                    case "WORD":
                        ViewModels.Add(viewModelFactory.CreateViewModel<ISymbol, SymbolObservationViewModel<ushort>>(symbol));
                        break;
                    case "DWORD":
                        ViewModels.Add(viewModelFactory.CreateViewModel<ISymbol, SymbolObservationViewModel<uint>>(symbol));
                        break;
                    case "SINT":
                        ViewModels.Add(viewModelFactory.CreateViewModel<ISymbol, SymbolObservationViewModel<sbyte>>(symbol));
                        break;
                    case "USINT":
                        ViewModels.Add(viewModelFactory.CreateViewModel<ISymbol, SymbolObservationViewModel<byte>>(symbol));
                        break;
                    case "INT":
                        ViewModels.Add(viewModelFactory.CreateViewModel<ISymbol, SymbolObservationViewModel<short>>(symbol));
                        break;
                    case "UINT":
                        ViewModels.Add(viewModelFactory.CreateViewModel<ISymbol, SymbolObservationViewModel<ushort>>(symbol));
                        break;
                    case "DINT":
                        ViewModels.Add(viewModelFactory.CreateViewModel<ISymbol, SymbolObservationViewModel<int>>(symbol));
                        break;
                    case "UDINT":
                        ViewModels.Add(viewModelFactory.CreateViewModel<ISymbol, SymbolObservationViewModel<uint>>(symbol));
                        break;
                    case "REAL":
                        ViewModels.Add(viewModelFactory.CreateViewModel<ISymbol, SymbolObservationViewModel<float>>(symbol));
                        break;
                    case "LREAL":
                        ViewModels.Add(viewModelFactory.CreateViewModel<ISymbol, SymbolObservationViewModel<double>>(symbol));
                        break;
                    case "STRING":
                        ViewModels.Add(viewModelFactory.CreateViewModel<ISymbol, SymbolObservationViewModel<string>>(symbol));
                        break;
                    case "DATE_AND_TIME":
                        ViewModels.Add(viewModelFactory.CreateViewModel<ISymbol, SymbolObservationViewModel<DT>>(symbol));
                        break;
                    case "LTIME":
                        ViewModels.Add(viewModelFactory.CreateViewModel<ISymbol, SymbolObservationViewModel<LTIME>>(symbol));
                        break;
                    case "TIME":
                        ViewModels.Add(viewModelFactory.CreateViewModel<ISymbol, SymbolObservationViewModel<TIME>>(symbol));
                        break;
                    default:
                        if (symbol.TypeName.ToUpper().StartsWith("STRING"))
                        {
                            ViewModels.Add(viewModelFactory.CreateViewModel<ISymbol, SymbolObservationViewModel<string>>(symbol));
                            break;
                        }

                        ViewModels.Add(viewModelFactory.CreateViewModel<ISymbol, SymbolObservationDefaultViewModel>(symbol));
                        var exception = new NotImplementedException(Resources.ThisTypeIsNotImplemented);
                        Logger.Error(exception.Message, exception);
                        break;
                }
            }
            else
            {
                MessageBox.Show(string.Format(Resources.TheSymbol0HasAlreadyBeenAddedToTheListOfObservables, symbol?.InstanceName), "Symbol already observed", MessageBoxButton.OK);
            }
        }
    }
}