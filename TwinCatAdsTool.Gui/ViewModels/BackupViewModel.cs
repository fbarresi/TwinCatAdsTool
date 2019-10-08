using System;
using System.Reactive;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Threading.Tasks;
using System.Windows.Forms;
using Newtonsoft.Json.Linq;
using ReactiveUI;
using TwinCAT;
using TwinCatAdsTool.Interfaces.Extensions;
using TwinCatAdsTool.Interfaces.Services;

namespace TwinCatAdsTool.Gui.ViewModels
{
    public class BackupViewModel : ViewModelBase
    {
        private readonly IClientService clientService;
        private readonly IPersistentVariableService persistentVariableService;
        private readonly IProcessingService processingService;
        private string backupText;
        private readonly Subject<JObject> variableSubject = new Subject<JObject>();

        public BackupViewModel(IClientService clientService, IPersistentVariableService persistentVariableService, IProcessingService processingService)
        {
            this.clientService = clientService;
            this.persistentVariableService = persistentVariableService;
            this.processingService = processingService;
        }

        public override void Init()
        {
            variableSubject.ObserveOnDispatcher()
                .Do(o => BackupText = o.ToString())
                .Retry()
                .Subscribe()
                .AddDisposableTo(Disposables)
                ;

            Read = ReactiveCommand.CreateFromTask(ReadVariables, canExecute:clientService.ConnectionState.Select(state => state == ConnectionState.Connected))
                .AddDisposableTo(Disposables);

            Save = ReactiveCommand.CreateFromTask(SaveVariables, clientService.ConnectionState.Select(state => state == ConnectionState.Connected)).AddDisposableTo(Disposables);
        }

        private async Task<Unit> ReadVariables()
        {
            var persistentVariables = await persistentVariableService.ReadPersistentVariables(clientService.Client);
            variableSubject.OnNext(persistentVariables);
            return Unit.Default;
        }

        private async Task<Unit> SaveVariables()
        {
            SaveFileDialog saveFileDialog1 = new SaveFileDialog();
            saveFileDialog1.Filter = "Json|*.json";
            saveFileDialog1.Title = "Save an Json File";
            var result = saveFileDialog1.ShowDialog();
            if (result == DialogResult.OK || result == DialogResult.Yes)
            {
                //await processingService.Connect(PlcAddress, 851);
                await processingService.Process(saveFileDialog1.FileName);
            }
            return Unit.Default;
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

        public ReactiveCommand<Unit,Unit> Read { get; set; }
        public ReactiveCommand<Unit, Unit> Save { get; set; }
    }
}