using System;
using System.IO;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Threading.Tasks;
using System.Windows.Forms;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using ReactiveUI;
using TwinCAT;
using TwinCatAdsTool.Gui.Properties;
using TwinCatAdsTool.Interfaces.Extensions;
using TwinCatAdsTool.Interfaces.Services;

namespace TwinCatAdsTool.Gui.ViewModels
{
    public class BackupViewModel : ViewModelBase
    {
        private readonly IClientService clientService;
        private readonly IPersistentVariableService persistentVariableService;
        private readonly Subject<JObject> variableSubject = new Subject<JObject>();
        private string backupText;

        public BackupViewModel(IClientService clientService, IPersistentVariableService persistentVariableService)
        {
            this.clientService = clientService;
            this.persistentVariableService = persistentVariableService;
        }

        public string BackupText
        {
            get => backupText;
            set
            {
                if (value == backupText) return;
                backupText = value;
                raisePropertyChanged();
            }
        }

        public ReactiveCommand<Unit, Unit> Read { get; set; }
        public ReactiveCommand<Unit, Unit> Save { get; set; }

        public override void Init()
        {
            variableSubject
                .ObserveOnDispatcher()
                .Do(o => BackupText = o.ToString(Formatting.Indented))
                .Retry()
                .Subscribe()
                .AddDisposableTo(Disposables)
                ;

            Read = ReactiveCommand.CreateFromTask(ReadVariables, canExecute: clientService.ConnectionState.Select(state => state == ConnectionState.Connected))
                .AddDisposableTo(Disposables);

            Save = ReactiveCommand.CreateFromTask(SaveVariables, clientService.ConnectionState.Select(state => state == ConnectionState.Connected))
                .AddDisposableTo(Disposables);
        }

        private async Task<Unit> ReadVariables()
        {
            var persistentVariables = await persistentVariableService.ReadPersistentVariables(clientService.Client, clientService.TreeViewSymbols);
            variableSubject.OnNext(persistentVariables);
            Logger.Debug(Resources.ReadPersistentVariables);

            return Unit.Default;
        }

        private Task<Unit> SaveVariables()
        {
            SaveFileDialog saveFileDialog1 = new SaveFileDialog();
            saveFileDialog1.Filter = "Json|*.json";
            saveFileDialog1.Title = "Save in a json file";
            saveFileDialog1.FileName = $"Backup_{DateTime.Now:yyy-MM-dd-HHmmss}.json";
            var result = saveFileDialog1.ShowDialog();
            if (result == DialogResult.OK || result == DialogResult.Yes)
            {
                File.WriteAllText(saveFileDialog1.FileName, BackupText);
                Logger.Debug(string.Format(Resources.SavedBackupTo0Logging, saveFileDialog1.FileName));
            }

            return Task.FromResult(Unit.Default);
        }
    }
}