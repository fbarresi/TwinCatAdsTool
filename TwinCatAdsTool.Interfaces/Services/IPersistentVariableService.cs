using System;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using TwinCAT.Ads;
using TwinCAT.TypeSystem;
using TwinCAT.TypeSystem.Generic;

namespace TwinCatAdsTool.Interfaces.Services
{
    public interface IPersistentVariableService
    {
        Task<JObject> ReadPersistentVariables(AdsClient client, IInstanceCollection<ISymbol> symbols1);
        IObservable<string> CurrentTask { get; }
    }
}