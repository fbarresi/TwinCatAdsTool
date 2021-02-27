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
        AdsClient Client { get; }
        IObservable<ConnectionState> ConnectionState { get; }
        IObservable<string> AdsState { get; }
        ISymbolCollection<ISymbol> TreeViewSymbols { get; }
        ISymbolCollection<ISymbol> FlatViewSymbols { get; }
        IObservable<IEnumerable<NetId>> DevicesFound { get; }
        Task Reload();
        Task Disconnect();
    }
}