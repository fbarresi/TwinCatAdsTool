using System;
using System.Threading.Tasks;
using TwinCAT;
using TwinCAT.Ads;

namespace TwinCatAdsTool.Interfaces.Services
{
    public interface IClientService
    {
        Task Connect(string amsNetId, int port = 851);
        TcAdsClient Client { get; }
        IObservable<ConnectionState> ConnectionState { get; }
    }
}