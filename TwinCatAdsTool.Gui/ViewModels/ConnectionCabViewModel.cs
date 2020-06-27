using System;
using System.Collections.ObjectModel;
using System.Diagnostics.CodeAnalysis;
using System.Reactive.Linq;
using DynamicData;
using System.Linq;
using System.Reactive;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;
using ReactiveUI;
using TwinCAT;
using TwinCAT.Ads;
using TwinCatAdsTool.Gui.Extensions;
using TwinCatAdsTool.Gui.Properties;
using TwinCatAdsTool.Interfaces.Extensions;
using TwinCatAdsTool.Interfaces.Models;
using TwinCatAdsTool.Interfaces.Services;

namespace TwinCatAdsTool.Gui.ViewModels
{
    public class ConnectionCabViewModel : ViewModelBase
    {
        private readonly IClientService clientService;
        private ObservableAsPropertyHelper<ConnectionState> connectionStateHelper;
        private int port = 851;
        private string selectedNetId;
        private NetId selectedAmsNetId;
        private ObservableAsPropertyHelper<string> adsStatusHelper;


        public ConnectionCabViewModel(IClientService clientService)
        {
            this.clientService = clientService;
        }

        public ObservableCollection<NetId> AmsNetIds { get; set; } = new ObservableCollection<NetId>();
        public ReactiveCommand<Unit, Unit> Connect { get; set; }

        public ConnectionState ConnectionState => connectionStateHelper.Value;
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

        public NetId SelectedAmsNetId
        {
            get { return selectedAmsNetId; }

            set
            {
                if (selectedAmsNetId != value)
                {
                    selectedAmsNetId = value;
                    raisePropertyChanged();
                }
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


        public override void Init()
        {
            Connect = ReactiveCommand.CreateFromTask(ConnectClient, canExecute: clientService.ConnectionState.Select(state => state != ConnectionState.Connected))
                .AddDisposableTo(Disposables).SetupErrorHandling(Logger, Disposables);
            Disconnect = ReactiveCommand.CreateFromTask(DisconnectClient, canExecute: clientService.ConnectionState.Select(state => state == ConnectionState.Connected))
                .AddDisposableTo(Disposables).SetupErrorHandling(Logger, Disposables);
            
            connectionStateHelper = clientService
                .ConnectionState
                .ObserveOnDispatcher()
                .ToProperty(this, model => model.ConnectionState);

            adsStatusHelper = clientService
                .AdsState
                .ObserveOnDispatcher()
                .ToProperty(this, model => model.AdsStatus);


            AmsNetIds.AddRange(clientService.AmsNetIds);
            AmsNetIds.Add(new NetId(){Address = "", Name = "*"});
            SelectedAmsNetId = AmsNetIds.FirstOrDefault();

            this.WhenAnyValue(vm => vm.SelectedAmsNetId)
                .ObserveOn(Dispatcher.CurrentDispatcher)
                .Do(s => SelectedNetId = s.Address)
                .Subscribe()
                .AddDisposableTo(Disposables);
            
        }

        public string AdsStatus => adsStatusHelper.Value;

        private async Task ConnectClient()
        {
            try
            {
                await clientService.Connect(SelectedNetId, Port);
                Logger.Debug(string.Format(Resources.ClientConnectedToDevice0WithAddress1, SelectedAmsNetId?.Name,
                    SelectedAmsNetId?.Address));
            }
            catch (AdsInitializeException ex) when (ex.InnerException is DllNotFoundException && ex.InnerException.Source == "TwinCAT.Ads")
            {
                Logger.Error("Dll not found TwinCAT.Ads");
                MessageBox.Show("Dll for TwinCAT.Ads not found. Have you installed the drivers?");
            }
        }

        private async Task DisconnectClient()
        {
            await clientService.Disconnect();
            Logger.Debug("Client disconnected");
        }
    }
}