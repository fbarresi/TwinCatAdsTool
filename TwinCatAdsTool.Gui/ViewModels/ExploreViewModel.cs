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
using TwinCatAdsTool.Interfaces.Services;

namespace TwinCatAdsTool.Gui.ViewModels
{
    public class ExploreViewModel : ViewModelBase
    {
        private readonly IClientService clientService;

        private readonly IPersistentVariableService persistentVariableService;
        private readonly IViewModelFactory viewModelFactory;
        private readonly ISelectionService<ISymbol> symbolSelection;

        private readonly Subject<ReadOnlySymbolCollection> variableSubject = new Subject<ReadOnlySymbolCollection>();
        private ISymbolLoader loader;

        private ObservableCollection<IValueSymbol> observedSymbols;

        private string searchText;

        private ObservableCollection<ISymbol> treeNodes;


        public ExploreViewModel(IClientService clientService, IPersistentVariableService persistentVariableService, 
            IViewModelFactory viewModelFactory, ISelectionService<ISymbol> symbolSelection)
        {
            this.clientService = clientService;
            this.persistentVariableService = persistentVariableService;
            this.viewModelFactory = viewModelFactory;
            this.symbolSelection = symbolSelection;
        }

        public ReactiveCommand<ISymbol, Unit> AddObserverCmd { get; set; }

        public ObservableCollection<string> LogOutput { get; } = new ObservableCollection<string>();

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
                .Where((ev => SearchText == null || SearchText.Length < 4))
                .Throttle(TimeSpan.FromSeconds(3))
                .Merge(searchTextChanged
                           .Where(ev => SearchText != null && SearchText.Length >= 4)
                           .Throttle(TimeSpan.FromMilliseconds(400)))
                .Select(args => SearchText)
                .Merge(
                    TextBoxEnterCommand.Executed.Select(e => SearchText))
                .DistinctUntilChanged();

            // Log all events in the event stream to the Log viewer
            input.ObserveOnDispatcher()
                .Subscribe(e => LogOutput.Insert(0,
                                                 string.Format("Text Changed. Current Value - {0}", e)));

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
                        LogOutput.Insert(0, string.Format("Search for '{0}' returned '{1}' items", result.SearchTerm, result.Results.Count()));


                        result.Results.ToList().ForEach(item => SearchResults.Add(item));
                    }
                );
        }

        private Task<Unit> RegisterSymbolObserver(ISymbol symbol)
        {
            symbolSelection.Select(symbol);
            return Task.FromResult(Unit.Default);
        }

        private SearchResult DoSearch(string searchTerm)
        {
            var searchResult = new SearchResult {Results = new List<ISymbol>(), SearchTerm = searchTerm};
            if (searchTerm.Length < 5)
            {
                return searchResult;
            }

            var iterator = new SymbolIterator(loader.Symbols, s => s.InstancePath.ToLower().Contains(searchTerm.ToLower()));
            searchResult.Results = iterator;
            return searchResult;
        }

        private async Task<Unit> ReadVariables()
        {
            loader = SymbolLoaderFactory.Create(clientService.Client, new SymbolLoaderSettings(SymbolsLoadMode.VirtualTree));

            variableSubject.OnNext(loader.Symbols);
            return Unit.Default;
        }

        private void UpdateTree(ReadOnlySymbolCollection symbolList)
        {
            //DisplayTreeView(json.Root, Path.GetFileNameWithoutExtension(json.Path));
            try
            {
                TreeNodes.Clear();
                foreach (var s in symbolList)
                {
                    TreeNodes.Add(s);
                }
            }
            finally
            {
                raisePropertyChanged("TreeNodes");
            }
        }

        //private void DisplayTreeView(JToken root, string rootName)
        //{
        //    try
        //    {
        //        TreeNodes.Clear();
        //        var rootNode = new TreeViewModel("Root");
        //        rootNode.FullPath = "Root";
        //        TreeNodes.Add(rootNode);
        //        AddNode(root, rootNode, "");
        //    }
        //    finally
        //    {
        //        raisePropertyChanged("TreeNodes");
        //    }
        //}

        //private void AddNode(JToken token, TreeViewModel inTreeNode, string path)
        //{
        //    if (token == null)
        //        return;
        //    if (token is JValue)
        //    {
        //        var childNode = new TreeViewModel(token.ToString());
        //        childNode.FullPath = path == "" ? childNode.Name : (path + $".{childNode.Name}");
        //        inTreeNode.Children.Add(childNode);
        //    }
        //    else if (token is JObject)
        //    {
        //        var obj = (JObject)token;
        //        foreach (var property in obj.Properties())
        //        {
        //            var childNode = new TreeViewModel(property.Name);
        //            childNode.FullPath = path == "" ? childNode.Name : (path + $".{childNode.Name}");
        //                inTreeNode.Children.Add(childNode);
        //            AddNode(property.Value, childNode, childNode.FullPath);
        //        }
        //    }
        //    else if (token is JArray)
        //    {
        //        var array = (JArray)token;
        //        for (int i = 0; i < array.Count; i++)
        //        {
        //            var childNode = new TreeViewModel(i.ToString());
        //            childNode.FullPath = path == "" ? childNode.Name : (path + $".{childNode.Name}");
        //                inTreeNode.Children.Add(childNode);
        //            AddNode(array[i], childNode, childNode.FullPath);
        //        }
        //    }
        //    else
        //    {
        //        Debug.WriteLine(string.Format("{0} not implemented", token.Type)); // JConstructor, JRaw
        //    }
        //}
    }
}