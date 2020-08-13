using System;
using System.Collections.Generic;
using System.Net;
using System.Threading.Tasks;
using TwinCAT;
using TwinCAT.Ads;
using TwinCAT.TypeSystem;
using TwinCAT.TypeSystem.Generic;
using TwinCatAdsTool.Interfaces.Models;

namespace TwinCatAdsTool.Interfaces.Services
{
    public interface IClientService
    {
        Task Connect(string amsNetId, int port);
        TcAdsClient Client { get; }
        IObservable<ConnectionState> ConnectionState { get; }
        IObservable<string> AdsState { get; }
        ReadOnlySymbolCollection TreeViewSymbols { get; }
        ReadOnlySymbolCollection FlatViewSymbols { get; }
        IObservable<IEnumerable<NetId>> DevicesFound { get; }
        Task Reload();
        Task Disconnect();
    }
}