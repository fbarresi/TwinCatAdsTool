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
        Task Connect(AmsNetId amsNetId, int port = 851);
        TcAdsClient Client { get; }
        IObservable<ConnectionState> ConnectionState { get; }
        ReadOnlySymbolCollection TreeViewSymbols { get; }
        ReadOnlySymbolCollection FlatViewSymbols { get; }
        List<NetId> AmsNetIds { get; set; }
        Task Reload();

    }
}