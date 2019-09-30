using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using TwinCAT.Ads;

namespace TwinCatAdsTool.Interfaces.Services
{
    public interface IPersistentVariableService
    {
        Task<JObject> ReadPersistentVariables(TcAdsClient client);
    }
}