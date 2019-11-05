﻿using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Net;
using System.Reactive;
using System.Reactive.Linq;
using System.Threading.Tasks;
using System.Windows.Documents;
using DynamicData;
using ReactiveUI;
using TwinCAT;
using TwinCAT.Ads;
using TwinCatAdsTool.Interfaces.Extensions;
using TwinCatAdsTool.Interfaces.Models;
using TwinCatAdsTool.Interfaces.Services;

namespace TwinCatAdsTool.Gui.ViewModels
{
    public class ConnectionCabViewModel : ViewModelBase
    {
        private readonly IClientService clientService;
        private int port = 851;
        private ObservableAsPropertyHelper<ConnectionState> connectionStateHelper;
        private NetId selectedAmsNetId = null;
        public ObservableCollection<NetId> AmsNetIds { get; set; } = new ObservableCollection<NetId>();

        public NetId SelectedAmsNetId
        {
            get { return selectedAmsNetId; }

            set
            {
                if (selectedAmsNetId != value)
                {
                    selectedAmsNetId = value;
                    raisePropertyChanged("SelectedAmsNetId");
                }
            }
        }


        public ConnectionCabViewModel(IClientService clientService)
        {
            this.clientService = clientService;
        }

        public override void Init()
        {
            Connect = ReactiveCommand.CreateFromTask(ConnectClient, canExecute: clientService.ConnectionState.CombineLatest(this.WhenAnyValue(vm => vm.SelectedAmsNetId), (state, amsNetId) => state != ConnectionState.Connected && amsNetId != null))
                .AddDisposableTo(Disposables);
            Disconnect = ReactiveCommand.CreateFromTask(DisconnectClient, canExecute: IsConnected)
                .AddDisposableTo(Disposables);

            connectionStateHelper = clientService
                .ConnectionState
                .ObserveOnDispatcher()
                .ToProperty(this, model => model.ConnectionState);


            AmsNetIds.AddRange(clientService.AmsNetIds);
            SelectedAmsNetId = AmsNetIds.FirstOrDefault();
        }

        public IObservable<bool> IsConnected { get; set; }

        private Task<Unit> ConnectClient()
        {
            clientService.Client.Connect(SelectedAmsNetId.Address, Port);
            Logger.Debug($"Client connected to device {SelectedAmsNetId?.Name} with address  {SelectedAmsNetId?.Address} ");
            return Task.FromResult(Unit.Default);
        }

        private Task<Unit> DisconnectClient()
        {
            clientService.Client.Disconnect();
            Logger.Debug($"Client disconnected");
            return Task.FromResult(Unit.Default);
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