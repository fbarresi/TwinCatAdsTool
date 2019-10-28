using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using TwinCAT;
using TwinCAT.Ads;
using TwinCAT.TypeSystem;
using TwinCAT.TypeSystem.Generic;

namespace TwinCatAdsTool.Interfaces.Services
{
    public interface IClientService
    {
        Task Connect(string amsNetId, int port = 851);
        TcAdsClient Client { get; }
        IObservable<ConnectionState> ConnectionState { get; }
        ReadOnlySymbolCollection TreeViewSymbols { get; }
        ReadOnlySymbolCollection FlatViewSymbols { get; }
        Task Reload();

    }
}