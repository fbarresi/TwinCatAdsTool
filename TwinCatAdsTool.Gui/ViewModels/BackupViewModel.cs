using System;
using System.Reactive;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Threading.Tasks;
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
        private string backupText;
        private readonly Subject<JObject> variableSubject = new Subject<JObject>();

        public BackupViewModel(IClientService clientService, IPersistentVariableService persistentVariableService)
        {
            this.clientService = clientService;
            this.persistentVariableService = persistentVariableService;
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
        }

        private async Task<Unit> ReadVariables()
        {
            var persistentVariables = await persistentVariableService.ReadPersistentVariables(clientService.Client);
            variableSubject.OnNext(persistentVariables);
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
    }
}