using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reactive;
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

        TreeNodes
            .ToObservableChangeSet()
            .ObserveOnDispatcher()
            .Subscribe()
            .AddDisposableTo(Disposables);

        
        Read = ReactiveCommand.CreateFromTask(ReadVariables, canExecute: clientService.ConnectionState.Select(state => state == ConnectionState.Connected))
            .AddDisposableTo(Disposables);

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



    private void DisplayTreeView(JToken root, string rootName)
    {
        // TreeView1.BeginUpdate();
        try
        {
            TreeNodes.Clear();
            //var rootNode = new TreeNode(rootName);
            var rootNode = new TreeViewModel("Root");
            TreeNodes.Add(rootNode);
            AddNode(root, rootNode);


            // TreeView1.ExpandAll();
        }
        finally
        {
            // TreeView1.EndUpdate();

            raisePropertyChanged("TreeNodes");
        }
    }
    private void AddNode(JToken token, TreeViewModel inTreeNode)
    {
        if (token == null)
            return;
        if (token is JValue)
        {
            var childNode = new TreeViewModel(token.ToString());
            inTreeNode.Children.Add(childNode);
        }
        else if (token is JObject)
        {
            var obj = (JObject)token;
            foreach (var property in obj.Properties())
            {
                var childNode = new TreeViewModel(property.Name);
                inTreeNode.Children.Add(childNode);
                AddNode(property.Value, childNode);
            }
        }
        else if (token is JArray)
        {
            var array = (JArray)token;
            for (int i = 0; i < array.Count; i++)
            {
                var childNode = new TreeViewModel(i.ToString());
                inTreeNode.Children.Add(childNode);
                AddNode(array[i], childNode);
            }
        }
        else
        {
            Debug.WriteLine(string.Format("{0} not implemented", token.Type)); // JConstructor, JRaw
        }
    }

    }
}
