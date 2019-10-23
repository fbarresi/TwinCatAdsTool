using System;
using System.Reactive;
using System.Reactive.Linq;
using System.Threading.Tasks;
using ReactiveUI;
using TwinCAT;
using TwinCatAdsTool.Interfaces.Extensions;
using TwinCatAdsTool.Interfaces.Services;

namespace TwinCatAdsTool.Gui.ViewModels
{
    public class ConnectionCabViewModel : ViewModelBase
    {
        private readonly IClientService clientService;
        private string address = "5.45.127.110.1.1";
        private int port = 851;
        private ObservableAsPropertyHelper<ConnectionState> connectionStateHelper;

        public ConnectionCabViewModel(IClientService clientService)
        {
            this.clientService = clientService;
        }

        public override void Init()
        {
            Connect = ReactiveCommand.CreateFromTask(ConnectClient, canExecute: clientService.ConnectionState.Select(state => state != ConnectionState.Connected))
                .AddDisposableTo(Disposables);
            Disconnect = ReactiveCommand.CreateFromTask(DisconnectClient, canExecute: clientService.ConnectionState.Select(state => state == ConnectionState.Connected))
                .AddDisposableTo(Disposables);

            connectionStateHelper = clientService
                .ConnectionState
                .ObserveOnDispatcher()
                .ToProperty(this, model => model.ConnectionState);

        }

        private Task<Unit> ConnectClient()
        {
            clientService.Client.Connect(Address, Port);
            return Task.FromResult(Unit.Default);
        }

        private Task<Unit> DisconnectClient()
        {
            clientService.Client.Disconnect();
            return Task.FromResult(Unit.Default);
        }

        public ConnectionState ConnectionState => connectionStateHelper.Value;
        public ReactiveCommand<Unit, Unit> Connect { get; set; }
        public ReactiveCommand<Unit, Unit> Disconnect { get; set; }

        public string Address
        {
            get => address;
            set
            {
                if (value == address) return;
                address = value;
                raisePropertyChanged();
            }
        }

        public int Port
        {
            get => port;
            set
            {
                if (value == port) return;
                port = value;
                raisePropertyChanged();
            }
        }
    }

}