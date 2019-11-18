using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Reactive.Linq;
using DynamicData;
using System.Linq;
using System.Reactive;
using System.Reactive.Subjects;
using System.Threading.Tasks;
using System.Windows;
using Microsoft.Win32;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using ReactiveUI;
using TwinCAT;
using TwinCAT.Ads;
using TwinCAT.JsonExtension;
using TwinCAT.PlcOpen;
using TwinCAT.TypeSystem;
using TwinCatAdsTool.Gui.Extensions;
using TwinCatAdsTool.Interfaces.Extensions;
using TwinCatAdsTool.Interfaces.Services;

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

            canWrite.Subscribe().AddDisposableTo(Disposables);


            Load = ReactiveCommand.CreateFromTask(LoadVariables, canExecute: clientService.ConnectionState.Select(state => state == ConnectionState.Connected))
                .AddDisposableTo(Disposables);

            Write = ReactiveCommand.CreateFromTask(WriteVariables, canWrite.Select(x => x))
                .AddDisposableTo(Disposables);
        }

        public async Task WriteJsonRecursive(TcAdsClient client, string name, JToken token)
        {
            Logger.Debug($"Trying to write JSON {name} with value {token}");
            var symbolInfo = (ITcAdsSymbol5) client.ReadSymbolInfo(name);
            var dataType = symbolInfo.DataType;
            if (dataType.Category == DataTypeCategory.Array)
            {
                var array = token as JArray;
                var elementCount = array.Count < dataType.Dimensions.ElementCount ? array.Count : dataType.Dimensions.ElementCount;
                if (dataType.BaseType.ManagedType == typeof(byte))
                {
                    byte[] bytes = Convert.FromBase64String(array[0].ToString());
                    for (int i = 0; i < bytes.Length; i++)
                    {
                        await client.WriteAsync(name + $"[{i + dataType.Dimensions.LowerBounds.First()}]", bytes[i]).ConfigureAwait(false);
                    }
                }
                else
                {
                    for (int i = 0; i < elementCount; i++)
                    {
                        if (dataType.BaseType.ManagedType != null)
                        {
                            await client.WriteAsync(name + $"[{i + dataType.Dimensions.LowerBounds.First()}]", array[i]).ConfigureAwait(false);
                        }
                        else
                        {
                            await WriteJsonRecursive(client, name + $"[{i + dataType.Dimensions.LowerBounds.First()}]", array[i]).ConfigureAwait(false);
                        }
                    }
                }
            }
            else if (dataType.ManagedType == null)
            {
                if (dataType.SubItems.Any())
                {
                    foreach (var subItem in dataType.SubItems)
                    {
                        await WriteJsonRecursive(client, name + "." + subItem.SubItemName, token.SelectToken(subItem.SubItemName)).ConfigureAwait(false);
                    }
                }
            }
            else if (dataType.ManagedType == typeof(TIME))
            {
                await client.WriteAsync(symbolInfo.Name, new TIME(token.ToObject<TimeSpan>())).ConfigureAwait(false);
            }
            else if (dataType.ManagedType == typeof(LTIME))
            {
                await client.WriteAsync(symbolInfo.Name, new LTIME(token.ToObject<TimeSpan>())).ConfigureAwait(false);
            }
            else if (dataType.ManagedType == typeof(DT))
            {
                await client.WriteAsync(symbolInfo.Name, new DT(token.ToObject<DateTime>())).ConfigureAwait(false);
            }
            else if (dataType.ManagedType == typeof(DATE))
            {
                await client.WriteAsync(symbolInfo.Name, new DATE(token.ToObject<DateTime>())).ConfigureAwait(false);
            }
            else
            {
                await client.WriteAsync(symbolInfo.Name, token).ConfigureAwait(false);
            }
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

        private async Task<Unit> LoadVariablesFromFile()
        {
            OpenFileDialog openFileDialog = new OpenFileDialog();
            openFileDialog.Filter = "Json files (*.json)|*.json";
            openFileDialog.InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
            if (openFileDialog.ShowDialog() == true)
            {
                JObject json = JObject.Parse(File.ReadAllText(openFileDialog.FileName));
                fileVariableSubject.OnNext(json);
                canWrite.OnNext(true);
            }

            return Unit.Default;
        }

        private async Task ReadVariablesFromPlc()
        {
            var persistentVariables = await persistentVariableService.ReadPersistentVariables(clientService.Client, clientService.TreeViewSymbols);
            liveVariableSubject.OnNext(persistentVariables);
        }

        private void UpdateDisplayIfMatching()
        {
            if (!FileVariables.HasEqualStructure(LiveVariables))
            {
                MessageBox.Show("File does not have the same structure as the Plc", "Error", MessageBoxButton.OK);
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

        private void UpdateVariables(JObject json, ObservableCollection<VariableViewModel> viewModels)
        {
            viewModels.Clear();
            AddVariable(json.Properties(), viewModels);
            Logger.Debug("Updated RestoreView");
        }

        private async Task<Unit> WriteVariables()
        {
            MessageBoxResult messageBoxResult = MessageBox.Show("Are you sure you want to overwrite the LiveVariables on the PLC?", "Overwrite Confirmation", MessageBoxButton.YesNo);
            if (messageBoxResult == MessageBoxResult.Yes)
            {
                foreach (var variable in DisplayVariables.Where(d => LiveVariables.Single(l => l.Name == d.Name).Json != d.Json))
                {
                    try
                    {
                        var jobject = JObject.Load(new JsonTextReader(new StringReader(variable.Json)));
                        foreach (var p in jobject.Properties())
                        {
                            var o = new JObject();
                            o.Add(p.Name, p.Value);
                            await WriteJsonRecursive(clientService.Client, variable.Name + "." + p.Name, p.Value).ConfigureAwait(false);
                        }
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show(ex.Message, ex.GetType().ToString(), MessageBoxButton.OK);
                    }
                }
            }

            return Unit.Default;
        }
    }
}