using System;
using TwinCAT;
using TwinCAT.Ads;

namespace TwinCatAdsTool.Interfaces.Services
{
    public interface IClientService
    {
        TcAdsClient Client { get; }
        IObservable<ConnectionState> ConnectionState { get; }
    }
}