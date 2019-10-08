using System;
using System.Reactive;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Threading.Tasks;
using Ninject;
using TwinCAT;
using TwinCAT.Ads;
using TwinCatAdsTool.Interfaces.Extensions;
using TwinCatAdsTool.Interfaces.Services;

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

        public Task Connect(string amsNetId, int port = 851)
        {
            if (!Client.IsConnected)
            {
                Client.Connect(amsNetId, port);
            }
            return Task.FromResult(Unit.Default);
        }

        public TcAdsClient Client { get; }
        public IObservable<ConnectionState> ConnectionState => connectionStateSubject.AsObservable();

        public void Initialize()
        {
            Observable.FromEventPattern<ConnectionStateChangedEventArgs>(ev => Client.ConnectionStateChanged += ev,
                                                                         ev => Client.ConnectionStateChanged -= ev)
                .Select(pattern => pattern.EventArgs)
                .Select(args => args.NewState)
                .Subscribe(connectionStateSubject.OnNext)
                .AddDisposableTo(disposables);

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