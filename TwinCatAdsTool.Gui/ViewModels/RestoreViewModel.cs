using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Threading.Tasks;
using System.Windows;
using DynamicData;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using ReactiveUI;
using TwinCAT;
using TwinCatAdsTool.Gui.Extensions;
using TwinCatAdsTool.Interfaces.Extensions;
using TwinCatAdsTool.Interfaces.Services;
using OpenFileDialog = Microsoft.Win32.OpenFileDialog;

namespace TwinCatAdsTool.Gui.ViewModels
{
    public class RestoreViewModel : ViewModelBase
    {
        private readonly IClientService clientService;
        private readonly IPersistentVariableService persistentVariableService;
        private readonly BehaviorSubject<JObject> liveVariableSubject = new BehaviorSubject<JObject>(new JObject());
        private readonly BehaviorSubject<JObject> fileVariableSubject = new BehaviorSubject<JObject>(new JObject());
        private ObservableCollection<VariableViewModel> liveVariables;
        private ObservableCollection<VariableViewModel> fileVariables;
        private ObservableCollection<VariableViewModel> displayVariables;

        public ObservableCollection<VariableViewModel> LiveVariables
        {
            get => liveVariables ?? (liveVariables = new ObservableCollection<VariableViewModel>());
            set
            {
                if (value == liveVariables) return;
                liveVariables = value;
                raisePropertyChanged();
            }
        }

        public ObservableCollection<VariableViewModel> FileVariables
        {
            get => fileVariables ?? (fileVariables = new ObservableCollection<VariableViewModel>());
            set
            {
                if (value == fileVariables) return;
                fileVariables = value;
                raisePropertyChanged();
            }
        }

        public ObservableCollection<VariableViewModel> DisplayVariables
        {
            get => displayVariables ?? (displayVariables = new ObservableCollection<VariableViewModel>());
            set
            {
                if (value == displayVariables) return;
                liveVariables = value;
                raisePropertyChanged();
            }
        }

        public RestoreViewModel(IClientService clientService, IPersistentVariableService persistentVariableService)
        {
            this.clientService = clientService;
            this.persistentVariableService = persistentVariableService;
        }

        public override void Init()
        {
            liveVariableSubject
                .ObserveOnDispatcher()
                .Do(x => UpdateVariables(x, LiveVariables))
                .Retry()
                .Subscribe()
                .AddDisposableTo(Disposables)
                ;

            fileVariableSubject
                .ObserveOnDispatcher()
                .Do(x => UpdateVariables(x, FileVariables))
                .Do(x => UpdateDisplayIfMatching())
                .Retry()
                .Subscribe()
                .AddDisposableTo(Disposables)
                ;

            Load = ReactiveCommand.CreateFromTask(LoadVariables, canExecute: clientService.ConnectionState.Select(state => state == ConnectionState.Connected))
                .AddDisposableTo(Disposables);

            Write = ReactiveCommand.CreateFromTask(WriteVariables, clientService.ConnectionState.Select(state => state == ConnectionState.Connected))
                .AddDisposableTo(Disposables);
        }

        private void UpdateVariables(JObject json, ObservableCollection<VariableViewModel> viewModels)
        {
            viewModels.Clear();
            AddVariable(json.Properties(), viewModels);
        }

        private void AddVariable(IEnumerable<JProperty> token, ObservableCollection<VariableViewModel> variables)
        {
            try
            {
                foreach (var prop in token)
                {
                    if (prop.Value is JObject)
                    {
                        var variable = new VariableViewModel();
                        variable.Name = prop.Name;
                        variable.Json = prop.Value.ToString();
                        variables.Add(variable);
                    }
                }

            }
            finally
            {
                raisePropertyChanged("LiveVariables");
            }
        }
     

        private async Task<Unit> LoadVariables()
        {
            await ReadVariablesFromPlc();
            await LoadVariablesFromFile();

            return Unit.Default;
        }

        private void UpdateDisplayIfMatching()
        {
            if (!FileVariables.HasEqualStructure(LiveVariables))
            {
                System.Windows.MessageBox.Show("File does not have the same structure as the Plc", "Error", System.Windows.MessageBoxButton.OK);
            }
            else
            {
                DisplayVariables.Clear();
                var array = new VariableViewModel[FileVariables.Count];
                FileVariables.CopyTo(array, 0);
                DisplayVariables.AddRange(array);

                raisePropertyChanged("DisplayVariables");
            }
        }

        private async Task<Unit> LoadVariablesFromFile()
        {
            Microsoft.Win32.OpenFileDialog openFileDialog = new OpenFileDialog();
            openFileDialog.Filter = "Json files (*.json)|*.json";
            openFileDialog.InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
            if (openFileDialog.ShowDialog() == true)
            {
                JObject json = JObject.Parse(File.ReadAllText(openFileDialog.FileName));
                fileVariableSubject.OnNext(json);
            }

            return Unit.Default;
        }

        private async Task ReadVariablesFromPlc()
        {
            var persistentVariables = await persistentVariableService.ReadPersistentVariables(clientService.Client);
            liveVariableSubject.OnNext(persistentVariables);
        }

        private async Task<Unit> WriteVariables()
        {
            MessageBoxResult messageBoxResult = System.Windows.MessageBox.Show("Are you sure you want to overwrite the LiveVariables on the PLC?", "Overwrite Confirmation", System.Windows.MessageBoxButton.YesNo);
            if (messageBoxResult == MessageBoxResult.Yes)
            {

                foreach (var variable in DisplayVariables.Where(d => LiveVariables.Single(l => l.Name == d.Name).Json != d.Json))
                {
                    try
                    {
                        var jobject = JObject.Load(new JsonTextReader(new StringReader(variable.Json)))  ;
                        //await clientService.Client.WriteJson(variable.Name, jobject, true);
                    }
                    catch (Exception ex)
                    {
                        System.Windows.MessageBox.Show(ex.Message, ex.GetType().ToString(), System.Windows.MessageBoxButton.OK);
                    }
                }
            }
            return Unit.Default;
        }

        public ReactiveCommand<Unit, Unit> Load { get; set; }
        public ReactiveCommand<Unit, Unit> Write { get; set; }
    }
}
