using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Reactive.Linq;
using DynamicData;
using System.Reactive;
using System.Reactive.Subjects;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Forms;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using ReactiveUI;
using TwinCAT;
using TwinCAT.JsonExtension;
using TwinCatAdsTool.Gui.Properties;
using TwinCatAdsTool.Interfaces.Extensions;
using TwinCatAdsTool.Interfaces.Services;
using MessageBox = System.Windows.MessageBox;
using OpenFileDialog = Microsoft.Win32.OpenFileDialog;

namespace TwinCatAdsTool.Gui.ViewModels
{
    public class RestoreViewModel : ViewModelBase
    {
        private readonly BehaviorSubject<bool> canWrite = new BehaviorSubject<bool>(false);
        private readonly IClientService clientService;
        private readonly BehaviorSubject<JObject> fileVariableSubject = new BehaviorSubject<JObject>(new JObject());
        private readonly BehaviorSubject<JObject> liveVariableSubject = new BehaviorSubject<JObject>(new JObject());
        private readonly IPersistentVariableService persistentVariableService;
        private ObservableCollection<VariableViewModel> displayVariables;
        private ObservableCollection<VariableViewModel> fileVariables;
        private ObservableCollection<VariableViewModel> liveVariables;

        public RestoreViewModel(IClientService clientService, IPersistentVariableService persistentVariableService)
        {
            this.clientService = clientService;
            this.persistentVariableService = persistentVariableService;
        }

        public ObservableCollection<VariableViewModel> DisplayVariables
        {
            get => displayVariables ?? (displayVariables = new ObservableCollection<VariableViewModel>());
            set
            {
                if (value == displayVariables)
                {
                    return;
                }

                liveVariables = value;
                raisePropertyChanged();
            }
        }

        public ObservableCollection<VariableViewModel> FileVariables
        {
            get => fileVariables ?? (fileVariables = new ObservableCollection<VariableViewModel>());
            set
            {
                if (value == fileVariables)
                {
                    return;
                }

                fileVariables = value;
                raisePropertyChanged();
            }
        }

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

        public ReactiveCommand<Unit, Unit> Load { get; set; }
        public ReactiveCommand<Unit, Unit> Write { get; set; }

        public override void Init()
        {
            fileVariableSubject
                .ObserveOnDispatcher()
                .Do(x => UpdateVariables(x, FileVariables))
                .Do(x => UpdateDisplayIfMatching())
                .Retry()
                .Subscribe()
                .AddDisposableTo(Disposables)
                ;

            canWrite.Subscribe().AddDisposableTo(Disposables);


            Load = ReactiveCommand.CreateFromTask(LoadVariables, canExecute: clientService.ConnectionState.Select(state => state == ConnectionState.Connected))
                .AddDisposableTo(Disposables);

            Write = ReactiveCommand.CreateFromTask(WriteVariables, canWrite.Select(x => x))
                .AddDisposableTo(Disposables);
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
            await LoadVariablesFromFile();

            return Unit.Default;
        }

        private Task<Unit> LoadVariablesFromFile()
        {
            OpenFileDialog openFileDialog = new OpenFileDialog();
            openFileDialog.Filter = "Json files (*.json)|*.json";
            openFileDialog.RestoreDirectory = true;
            if (openFileDialog.ShowDialog() == true)
            {
                JObject json = JObject.Parse(File.ReadAllText(openFileDialog.FileName));
                fileVariableSubject.OnNext(json);
                canWrite.OnNext(true);
            }

            return Task.FromResult(Unit.Default);
        }

        private void UpdateDisplayIfMatching()
        {
            DisplayVariables.Clear();
            var array = new VariableViewModel[FileVariables.Count];
            FileVariables.CopyTo(array, 0);
            DisplayVariables.AddRange(array);

            raisePropertyChanged("DisplayVariables");
        }

        private void UpdateVariables(JObject json, ObservableCollection<VariableViewModel> viewModels)
        {
            viewModels.Clear();
            AddVariable(json.Properties(), viewModels);
            Logger.Debug(Resources.UpdatedRestoreView);
        }

        private async Task<Unit> WriteVariables()
        {
            MessageBoxResult messageBoxResult = MessageBox.Show(Resources.AreYouSureYouWantToOverwriteTheLiveVariablesOnThePLC, Resources.OverwriteConfirmation, MessageBoxButton.YesNo);
            if (messageBoxResult == MessageBoxResult.Yes)
            {
                foreach (var variable in DisplayVariables)
                {
                    try
                    {
                        var jObject = JObject.Load(new JsonTextReader(new StringReader(variable.Json)));
                        foreach (var p in jObject.Properties())
                        {
                            Logger.Debug($"Restoring variable '{variable.Name}.{p.Name}' from backup...");
                            if(p.Value is JObject)
                                await clientService.Client.WriteJson(variable.Name + "." + p.Name, (JObject) p.Value, force: true);
                            else if(p.Value is JArray)
                                await clientService.Client.WriteJson(variable.Name + "." + p.Name, (JArray) p.Value, force: true);
                            else if (p.Value is JValue)
                                await clientService.Client.WriteAsync(variable.Name + "." + p.Name, p.Value);
                            else
                                Logger.Error($"Unable to write variable '{variable.Name}.{p.Name}' from backup: no type case match!");
                        }
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"{ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
            }

            return Unit.Default;
        }
    }
}