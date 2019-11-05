using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Linq.Expressions;
using System.Net;
using System.Net.Sockets;
using System.Reactive;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Threading.Tasks;
using Ninject;
using TwinCAT;
using TwinCAT.Ads;
using TwinCAT.Ads.TypeSystem;
using TwinCAT.TypeSystem;
using TwinCatAdsTool.Interfaces.Extensions;
using TwinCatAdsTool.Interfaces.Models;
using TwinCatAdsTool.Interfaces.Services;
using TwinCatAdsTool.Logic.Router;

namespace TwinCatAdsTool.Logic.Services
{
    public class ClientService : IClientService, IInitializable, IDisposable
    {
        private readonly BehaviorSubject<ConnectionState> connectionStateSubject = new BehaviorSubject<ConnectionState>(TwinCAT.ConnectionState.Unknown);
        private readonly CompositeDisposable disposables = new CompositeDisposable();
        public ClientService()
        {
            Client = new TcAdsClient();
        }


        public Task Connect(AmsNetId amsNetId, int port = 851)
        {
            if (!Client.IsConnected)
            {
                Client.Connect(amsNetId, port);
            }
            return Task.FromResult(Unit.Default);
        }

        public TcAdsClient Client { get; }
        public IObservable<ConnectionState> ConnectionState => connectionStateSubject.AsObservable();
        public ReadOnlySymbolCollection TreeViewSymbols { get; set; }
        public ReadOnlySymbolCollection FlatViewSymbols { get; set; }
        public List<NetId> AmsNetIds { get; set; }
        public Task Reload()
        {
            return Task.Run(() => UpdateSymbols(connectionStateSubject.Value));
        }

        public void Initialize()
        {
            Observable.FromEventPattern<ConnectionStateChangedEventArgs>(ev => Client.ConnectionStateChanged += ev,
                                                                         ev => Client.ConnectionStateChanged -= ev)
                .Select(pattern => pattern.EventArgs)
                .Select(args => args.NewState)
                .Subscribe(connectionStateSubject.OnNext)
                .AddDisposableTo(disposables);

            connectionStateSubject
                .DistinctUntilChanged()
                .Where(state => state == TwinCAT.ConnectionState.Connected)
                .Do(UpdateSymbols)
                .Subscribe()
                .AddDisposableTo(disposables);
  
            IPHostEntry host = Dns.GetHostEntry(Dns.GetHostName());

            var localhost = host
                .AddressList
                .FirstOrDefault(ip => ip.AddressFamily == AddressFamily.InterNetwork);
            AmsNetIds = DeviceFinder.BroadcastSearchAsync(localhost).Result.Select(x => new NetId{Name = x.Name, Address = x.AmsNetId.ToString()}).ToList();
        }

        private void UpdateSymbols(ConnectionState state)
        {
            if (state == TwinCAT.ConnectionState.Connected)
            {
                var loader = SymbolLoaderFactory.Create(Client, new SymbolLoaderSettings(SymbolsLoadMode.VirtualTree));
                TreeViewSymbols = loader.Symbols;

                var loader2 = SymbolLoaderFactory.Create(Client, new SymbolLoaderSettings(SymbolsLoadMode.Flat));
                FlatViewSymbols = loader2.Symbols;
            }
            else
            {
                TreeViewSymbols = null;
            }
        }

        public void Dispose()
        {
            Client.Disconnect();
            connectionStateSubject.OnNext(TwinCAT.ConnectionState.Disconnected);
            Client?.Dispose();
            disposables?.Dispose();
        }
    }
}