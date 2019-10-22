using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reactive;
using System.Reactive.Concurrency;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using DynamicData;
using DynamicData.Binding;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using ReactiveUI;
using TwinCAT;
using TwinCatAdsTool.Gui.Commands;
using TwinCatAdsTool.Gui.Extensions;
using TwinCatAdsTool.Interfaces.Extensions;
using TwinCatAdsTool.Interfaces.Services;

namespace TwinCatAdsTool.Gui.ViewModels
{
    public class ExploreViewModel : ViewModelBase
    {

        private readonly IClientService clientService;
    private readonly IPersistentVariableService persistentVariableService;
    private readonly BehaviorSubject<JObject> variableSubject = new BehaviorSubject<JObject>(new JObject());
    private ObservableCollection<TreeViewModel> treeNodes;

    private string searchText;


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

    private readonly ObservableCollection<string> logOutput = new ObservableCollection<string>();

    public ObservableCollection<string> LogOutput
    {
        get { return logOutput; }
    }

    private readonly ObservableCollection<TreeViewModel> searchResults = new ObservableCollection<TreeViewModel>();

    public ObservableCollection<TreeViewModel> SearchResults
    {
        get { return searchResults; }
    }


    public ExploreViewModel(IClientService clientService, IPersistentVariableService persistentVariableService)
    {
        this.clientService = clientService;
        this.persistentVariableService = persistentVariableService;
    }

    public ObservableCollection<TreeViewModel> TreeNodes
    {
        get => treeNodes ?? (treeNodes = new ObservableCollection<TreeViewModel>());
        set
        {
            if (value == treeNodes) return;
            treeNodes = value;
            raisePropertyChanged();
        }
    }

    public override void Init()
    {
        variableSubject
            .ObserveOnDispatcher()
            .Do(UpdateJson)
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
        textBoxEnterCommand = new ReactiveRelayCommand(obj => { });


            Read = ReactiveCommand.CreateFromTask(ReadVariables, canExecute: clientService.ConnectionState.Select(state => state == ConnectionState.Connected))
            .AddDisposableTo(Disposables);

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
                    textBoxEnterCommand.Executed.Select(e => SearchText))
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
              .Subscribe(result => {
                    searchResults.Clear();
                    LogOutput.Insert(0, string.Format("Search for '{0}' returned '{1}' items", result.SearchTerm, result.Results.Count()));


                    result.Results.ToList().ForEach(item => searchResults.Add(item));
                }
            );

    }


    private ReactiveRelayCommand textBoxEnterCommand;
    public ReactiveRelayCommand TextBoxEnterCommand
    {
        get { return textBoxEnterCommand; }
        set { textBoxEnterCommand = value; }
    }

    private async Task<Unit> ReadVariables()
    {
        var persistentVariables = await persistentVariableService.ReadPersistentVariables(clientService.Client);
        variableSubject.OnNext(persistentVariables);
        return Unit.Default;
    }

    public ReactiveCommand<Unit, Unit> Read { get; set; }

    private void UpdateJson(JObject json)
    {
        DisplayTreeView(json.Root, Path.GetFileNameWithoutExtension(json.Path));
    }

    private SearchResult DoSearch(string searchTerm)
    {

        var result =  new SearchResult
        {
            SearchTerm = searchTerm,
            Results =
                TreeNodes.Flatten().Where(item => item.Name.ToUpperInvariant().Contains(searchTerm.ToUpperInvariant())).ToArray()
        };
        return result;
    }

    private void DisplayTreeView(JToken root, string rootName)
    {
        try
        {
            TreeNodes.Clear();
            var rootNode = new TreeViewModel("Root");
            rootNode.FullPath = "Root";
            TreeNodes.Add(rootNode);
            AddNode(root, rootNode, "");
        }
        finally
        {
            raisePropertyChanged("TreeNodes");
        }
    }
    private void AddNode(JToken token, TreeViewModel inTreeNode, string path)
    {
        if (token == null)
            return;
        if (token is JValue)
        {
            var childNode = new TreeViewModel(token.ToString());
            childNode.FullPath = path == "" ? childNode.Name : (path + $".{childNode.Name}");
            inTreeNode.Children.Add(childNode);
        }
        else if (token is JObject)
        {
            var obj = (JObject)token;
            foreach (var property in obj.Properties())
            {
                var childNode = new TreeViewModel(property.Name);
                childNode.FullPath = path == "" ? childNode.Name : (path + $".{childNode.Name}");
                    inTreeNode.Children.Add(childNode);
                AddNode(property.Value, childNode, childNode.FullPath);
            }
        }
        else if (token is JArray)
        {
            var array = (JArray)token;
            for (int i = 0; i < array.Count; i++)
            {
                var childNode = new TreeViewModel(i.ToString());
                childNode.FullPath = path == "" ? childNode.Name : (path + $".{childNode.Name}");
                    inTreeNode.Children.Add(childNode);
                AddNode(array[i], childNode, childNode.FullPath);
            }
        }
        else
        {
            Debug.WriteLine(string.Format("{0} not implemented", token.Type)); // JConstructor, JRaw
        }
    }

    }
}
