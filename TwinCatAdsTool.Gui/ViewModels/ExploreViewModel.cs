using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Reactive.Linq;
using DynamicData.Binding;
using System.Linq;
using System.Reactive;
using System.Reactive.Subjects;
using System.Threading.Tasks;
using System.Windows;
using ReactiveUI;
using TwinCAT;
using TwinCAT.TypeSystem;
using TwinCatAdsTool.Gui.Commands;
using TwinCatAdsTool.Gui.Properties;
using TwinCatAdsTool.Interfaces.Commons;
using TwinCatAdsTool.Interfaces.Extensions;
using TwinCatAdsTool.Interfaces.Services;

namespace TwinCatAdsTool.Gui.ViewModels
{
    public class ExploreViewModel : ViewModelBase
    {
        private readonly IClientService clientService;
        private readonly ISelectionService<ISymbol> symbolSelection;

        private readonly Subject<ReadOnlySymbolCollection> variableSubject = new Subject<ReadOnlySymbolCollection>();

        private readonly IViewModelFactory viewModelFactory;
        private bool isConnected;
        private ObservableAsPropertyHelper<bool> isConnectedHelper;

        private ObservableCollection<IValueSymbol> observedSymbols;

        private string searchText;

        private ObservableCollection<ISymbol> treeNodes;


        public ExploreViewModel(IClientService clientService,
            IViewModelFactory viewModelFactory, ISelectionService<ISymbol> symbolSelection)
        {
            this.clientService = clientService;
            this.viewModelFactory = viewModelFactory;
            this.symbolSelection = symbolSelection;
        }

        public ReactiveCommand<ISymbol, Unit> AddObserverCmd { get; set; }

        public ReactiveCommand<SymbolObservationViewModel, Unit> CmdAddGraph { get; set; }
        public ReactiveCommand<SymbolObservationViewModel, Unit> CmdDelete { get; set; }

        public ReactiveCommand<SymbolObservationViewModel, Unit> CmdRemoveGraph { get; set; }
        public ReactiveCommand<SymbolObservationViewModel, Unit> CmdSubmit { get; set; }

        public GraphViewModel GraphViewModel { get; set; }

        public bool IsConnected
        {
            get { return isConnectedHelper.Value; }
            set
            {
                if (isConnectedHelper.Value == value)
                {
                    return;
                }

                isConnected = value;
                raisePropertyChanged();
            }
        }

        public ObservableCollection<IValueSymbol> ObservedSymbols
        {
            get => observedSymbols ?? (observedSymbols = new ObservableCollection<IValueSymbol>());
            set
            {
                if (value == observedSymbols) return;
                observedSymbols = value;
                raisePropertyChanged();
            }
        }

        public ObserverViewModel ObserverViewModel { get; set; }

        public ReactiveCommand<Unit, Unit> Read { get; set; }

        public ObservableCollection<ISymbol> SearchResults { get; } = new ObservableCollection<ISymbol>();


        public string SearchText
        {
            get { return searchText; }
            set
            {
                if (searchText == value)
                {
                    return;
                }

                searchText = value;
                raisePropertyChanged();
            }
        }

        public ReactiveRelayCommand TextBoxEnterCommand { get; set; }

        public ObservableCollection<ISymbol> TreeNodes
        {
            get => treeNodes ?? (treeNodes = new ObservableCollection<ISymbol>());
            set
            {
                if (value == treeNodes)
                {
                    return;
                }

                treeNodes = value;
                raisePropertyChanged();
            }
        }

        public override void Init()
        {
            ObserverViewModel = viewModelFactory.Create<ObserverViewModel>();
            ObserverViewModel.AddDisposableTo(Disposables);


            variableSubject
                .ObserveOnDispatcher()
                .Do(UpdateTree)
                .Retry()
                .Subscribe()
                .AddDisposableTo(Disposables);

            var treeNodeChangeSet = TreeNodes
                .ToObservableChangeSet()
                .ObserveOnDispatcher();

            treeNodeChangeSet
                .Subscribe()
                .AddDisposableTo(Disposables);

            var connected = clientService.ConnectionState.Select(state => state == ConnectionState.Connected);

            clientService.ConnectionState
                .DistinctUntilChanged()
                .Where(state => state == ConnectionState.Connected)
                .Do(_ => variableSubject.OnNext(clientService.TreeViewSymbols))
                .Subscribe()
                .AddDisposableTo(Disposables);

            connected.ToProperty(this, x => x.IsConnected, out isConnectedHelper);

            // Setup the command for the enter key on the textbox
            TextBoxEnterCommand = new ReactiveRelayCommand(obj => { });

            AddObserverCmd = ReactiveCommand.CreateFromTask<ISymbol, Unit>(RegisterSymbolObserver)
                .AddDisposableTo(Disposables);

            CmdDelete = ReactiveCommand.CreateFromTask<SymbolObservationViewModel, Unit>(DeleteSymbolObserver)
                .AddDisposableTo(Disposables);

            CmdSubmit = ReactiveCommand.CreateFromTask<SymbolObservationViewModel, Unit>(SubmitSymbol)
                .AddDisposableTo(Disposables);

            CmdAddGraph = ReactiveCommand.CreateFromTask<SymbolObservationViewModel, Unit>(AddGraph)
                .AddDisposableTo(Disposables);

            CmdRemoveGraph = ReactiveCommand.CreateFromTask<SymbolObservationViewModel, Unit>(RemoveGraph)
                .AddDisposableTo(Disposables);

            Read = ReactiveCommand.CreateFromTask(ReadVariables, canExecute: connected)
                .AddDisposableTo(Disposables);

            GraphViewModel = viewModelFactory.CreateViewModel<GraphViewModel>();
            GraphViewModel.AddDisposableTo(Disposables);

            this.WhenAnyValue(x => x.ObservedSymbols).Subscribe().AddDisposableTo(Disposables);

            // Listen to all property change events on SearchText
            var searchTextChanged = Observable.FromEventPattern<PropertyChangedEventHandler, PropertyChangedEventArgs>(
                        ev => PropertyChanged += ev,
                        ev => PropertyChanged -= ev
                    )
                    .Where(ev => ev.EventArgs.PropertyName == "SearchText")
                ;

            // Transform the event stream into a stream of strings (the input values)
            var input = searchTextChanged
                .Where((ev => SearchText == null || SearchText.Length < 5))
                .Throttle(TimeSpan.FromSeconds(3))
                .Merge(searchTextChanged
                           .Where(ev => SearchText != null && SearchText.Length >= 5)
                           .Throttle(TimeSpan.FromMilliseconds(400)))
                .Select(args => SearchText)
                .Merge(
                    TextBoxEnterCommand.Executed.Select(e => SearchText))
                .DistinctUntilChanged();

            // Setup an Observer for the search operation
            var search = Observable.ToAsync<string, SearchResult>(DoSearch);


            // Chain the input event stream and the search stream, cancelling searches when input is received
            var results = from searchTerm in input
                from result in search(searchTerm).TakeUntil(input)
                select result;


            // Log the search result and add the results to the results collection
            results
                .ObserveOnDispatcher()
                .Subscribe(result =>
                    {
                        SearchResults.Clear();
                        result.Results.ToList().ForEach(item => SearchResults.Add(item));
                    }
                );
        }

        private Task<Unit> AddGraph(SymbolObservationViewModel symbolObservationViewModel)
        {
            GraphViewModel.AddSymbol(symbolObservationViewModel);
            return Task.FromResult(Unit.Default);
        }

        private Task<Unit> DeleteSymbolObserver(SymbolObservationViewModel model)
        {
            try
            {
                ObserverViewModel.ViewModels.Remove(model);
                RemoveGraph(model);
            }
            catch (Exception ex)
            {
                Logger.Error(string.Format(Resources.CouldNotDeleteObserverForSymbol0, model?.Name), ex);
                MessageBox.Show(ex.Message, ex.GetType().ToString(), MessageBoxButton.OK);
            }

            return Task.FromResult(Unit.Default);
        }

        private SearchResult DoSearch(string searchTerm)
        {
            var searchResult = new SearchResult {Results = new List<ISymbol>(), SearchTerm = searchTerm};
            try
            {
                var iterator = new SymbolIterator(clientService.FlatViewSymbols, s => s.InstancePath.ToLower().Contains(searchTerm.ToLower()));
                searchResult.Results = iterator;
            }
            catch (Exception ex)
            {
                Logger.Error(Resources.ErrorDuringSearch, ex);
                MessageBox.Show(ex.Message, ex.GetType().ToString(), MessageBoxButton.OK);
            }

            return searchResult;
        }

        private async Task<Unit> ReadVariables()
        {
            try
            {
                await clientService.Reload();
            }
            catch (Exception ex)
            {
                Logger.Error(Resources.CouldNotReloadVariables, ex);
                MessageBox.Show(ex.Message, ex.GetType().ToString(), MessageBoxButton.OK);
            }

            return Unit.Default;
        }


        private Task<Unit> RegisterSymbolObserver(ISymbol symbol)
        {
            try
            {
                if (symbol.SubSymbols.Any())
                {
                    return Task.FromResult(Unit.Default);
                }

                if (symbol.DataType.IsContainer)
                {
                    return Task.FromResult(Unit.Default);
                }

                symbolSelection.Select(symbol);
            }
            catch (Exception ex)
            {
                Logger.Error(string.Format(Resources.CouldNotRegisterObserverForSymbol0, symbol?.InstanceName), ex);
                MessageBox.Show(ex.Message, ex.GetType().ToString(), MessageBoxButton.OK);
            }

            return Task.FromResult(Unit.Default);
        }

        private Task<Unit> RemoveGraph(SymbolObservationViewModel symbolObservationViewModel)
        {
            GraphViewModel.RemoveSymbol(symbolObservationViewModel);
            return Task.FromResult(Unit.Default);
        }

        private Task<Unit> SubmitSymbol(SymbolObservationViewModel model)
        {
            return Task.FromResult(Unit.Default);
        }

        private void UpdateTree(ReadOnlySymbolCollection symbolList)
        {
            try
            {
                TreeNodes.Clear();
                foreach (var s in symbolList)
                {
                    TreeNodes.Add(s);
                }
            }
            catch (Exception ex)
            {
                Logger.Error(Resources.CouldNotUpdateTree, ex);
                MessageBox.Show(ex.Message, ex.GetType().ToString(), MessageBoxButton.OK);
            }
            finally
            {
                raisePropertyChanged("TreeNodes");
            }
        }
    }
}