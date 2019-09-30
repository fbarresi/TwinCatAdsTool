using Newtonsoft.Json.Linq;
using TwinCAT.Ads;

namespace TwinCatAdsTool.Interfaces.Services
{
    public interface IPersistentVariableService
    {
        JObject ReadPersistentVariables(TcAdsClient client);
    }
}