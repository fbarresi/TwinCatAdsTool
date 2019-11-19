using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Net;
using System.Reactive;
using System.Reactive.Linq;
using System.Threading.Tasks;
using System.Windows.Documents;
using System.Windows.Threading;
using DynamicData;
using ReactiveUI;
using TwinCAT;
using TwinCAT.Ads;
using TwinCatAdsTool.Interfaces.Extensions;
using TwinCatAdsTool.Interfaces.Models;
using TwinCatAdsTool.Interfaces.Services;

namespace TwinCatAdsTool.Gui.ViewModels
{
    [SuppressMessage("ReSharper", "InvokeAsExtensionMethod")]
    public class ConnectionCabViewModel : ViewModelBase
    {
        private readonly IClientService clientService;
        private int port = 851;
        private ObservableAsPropertyHelper<ConnectionState> connectionStateHelper;
        private NetId selectedAmsNetId = null;
        private string selectedNetId;
        public ObservableCollection<NetId> AmsNetIds { get; set; } = new ObservableCollection<NetId>();

        public NetId SelectedAmsNetId
        {
            get { return selectedAmsNetId; }

            set
            {
                selectedAmsNetId = value;
                raisePropertyChanged();
            }
        }


        public string SelectedNetId
        {
            get => selectedNetId;
            set
            {
                if (selectedNetId != value)
                {
                    selectedNetId = value;
                    raisePropertyChanged();
                }
            }
        }

        public ConnectionCabViewModel(IClientService clientService)
        {
            this.clientService = clientService;
        }

        public override void Init()
        {
            var canConnect = Observable.CombineLatest(clientService.ConnectionState.StartWith(ConnectionState.Disconnected),
                                                      this.WhenAnyValue(vm => vm.SelectedAmsNetId),
                                                      (state, amsNetId) => state != ConnectionState.Connected && amsNetId != null)
                .ObserveOnDispatcher();

            Connect = ReactiveCommand.CreateFromTask(ConnectClient, canExecute: canConnect)
                .AddDisposableTo(Disposables);

            var canDisconnect = clientService.ConnectionState.StartWith(ConnectionState.Disconnected)
                .Select(state => state == ConnectionState.Connected)
                .ObserveOnDispatcher();
            Disconnect = ReactiveCommand.CreateFromTask(DisconnectClient, canExecute: 
                                                        canDisconnect)
                .AddDisposableTo(Disposables);

            connectionStateHelper = clientService
                .ConnectionState
                .ObserveOnDispatcher()
                .ToProperty(this, model => model.ConnectionState);


            AmsNetIds.AddRange(clientService.AmsNetIds);
            AmsNetIds.Add(new NetId(){Address = "", Name = "*"});
            SelectedAmsNetId = AmsNetIds.FirstOrDefault();

            this.WhenAnyValue(vm => vm.SelectedAmsNetId)
                .ObserveOn(Dispatcher.CurrentDispatcher)
                .Do(s => SelectedNetId = s.Address)
                .Subscribe()
                .AddDisposableTo(Disposables);
        }

        public IObservable<bool> IsConnected { get; set; }

        private async Task ConnectClient()
        {
            await clientService.Connect(SelectedAmsNetId.Address, Port);
            Logger.Debug($"Client connected to device {SelectedAmsNetId?.Name} with address  {SelectedAmsNetId?.Address} ");
        }

        private async Task DisconnectClient()
        {
            await clientService.Disconnect();
            Logger.Debug($"Client disconnected");
        }

        public ConnectionState ConnectionState => connectionStateHelper.Value;
        public ReactiveCommand<Unit, Unit> Connect { get; set; }
        public ReactiveCommand<Unit, Unit> Disconnect { get; set; }

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