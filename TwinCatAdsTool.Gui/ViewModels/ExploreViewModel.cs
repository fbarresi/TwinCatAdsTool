using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Reactive.Linq;
using DynamicData.Binding;
using System.Linq;
using System.Reactive;
using System.Reactive.Disposables;
using System.Reactive.Subjects;
using System.Threading.Tasks;
using ReactiveUI;
using TwinCAT;
using TwinCAT.Ads;
using TwinCAT.Ads.Reactive;
using TwinCAT.Ads.TypeSystem;
using TwinCAT.TypeSystem;
using TwinCatAdsTool.Gui.Commands;
using TwinCatAdsTool.Interfaces.Commons;
using TwinCatAdsTool.Interfaces.Extensions;
using TwinCatAdsTool.Interfaces.Logging;
using TwinCatAdsTool.Interfaces.Services;
using ListEx = DynamicData.ListEx;

namespace TwinCatAdsTool.Gui.ViewModels
{
    public class ExploreViewModel : ViewModelBase
    {
        private readonly IClientService clientService;

        private readonly IViewModelFactory viewModelFactory;
        private readonly ISelectionService<ISymbol> symbolSelection;

        private readonly Subject<ReadOnlySymbolCollection> variableSubject = new Subject<ReadOnlySymbolCollection>();
        private ISymbolLoader loader;

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
        public ReactiveCommand<SymbolObservationViewModel, Unit> CmdDelete { get; set; }

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
                if (value == treeNodes) return;
                treeNodes = value;
                raisePropertyChanged();
            }
        }

        public ObserverViewModel ObserverViewModel { get; set; }

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

            // Setup the command for the enter key on the textbox
            TextBoxEnterCommand = new ReactiveRelayCommand(obj => { });

            AddObserverCmd = ReactiveCommand.CreateFromTask<ISymbol, Unit>(RegisterSymbolObserver)
                .AddDisposableTo(Disposables);

            CmdDelete = ReactiveCommand.CreateFromTask<SymbolObservationViewModel, Unit>(DeleteSymbolObserver)
                .AddDisposableTo(Disposables);

            Read = ReactiveCommand.CreateFromTask(ReadVariables, canExecute: clientService.ConnectionState.Select(state => state == ConnectionState.Connected))
                .AddDisposableTo(Disposables);

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

        private Task<Unit> RegisterSymbolObserver(ISymbol symbol)
        {
            try
            {
                symbolSelection.Select(symbol);
            }
            catch (Exception ex)
            {
                Logger.Error($"Could not register Observer for Symbol {symbol?.InstanceName}", ex);
            }

            return Task.FromResult(Unit.Default);
        }

        private Task<Unit> DeleteSymbolObserver(SymbolObservationViewModel model)
        {
            try
            {
                ObserverViewModel.ViewModels.Remove(model);
            }catch(Exception ex)
            {
                Logger.Error($"Could not delete Observer for symbol {model?.Name}", ex);
            }

            return Task.FromResult(Unit.Default);
        }

        private SearchResult DoSearch(string searchTerm)
        {
            var searchResult = new SearchResult {Results = new List<ISymbol>(), SearchTerm = searchTerm};
            try
            {
                if (searchTerm.Length < 5)
                {
                    return searchResult;
                }

                var iterator = new SymbolIterator(loader.Symbols, s => s.InstancePath.ToLower().Contains(searchTerm.ToLower()));
                searchResult.Results = iterator;
            }catch(Exception ex)
            {
                Logger.Error("Error during search", ex);
            }

            return searchResult;
        }

        private async Task<Unit> ReadVariables()
        {
            try
            {
                loader = SymbolLoaderFactory.Create(clientService.Client, new SymbolLoaderSettings(SymbolsLoadMode.VirtualTree));

                variableSubject.OnNext(loader.Symbols);
            }catch(Exception ex)
            {
                Logger.Error("Could not read variables", ex);
            }

            return Unit.Default;
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
            }catch(Exception ex)
            {
                Logger.Error("Could not update Tree", ex);
            }
            finally
            {
                raisePropertyChanged("TreeNodes");
            }
        }
    }
}