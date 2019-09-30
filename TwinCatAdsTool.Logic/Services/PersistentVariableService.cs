using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using TwinCAT.Ads;
using TwinCAT.Ads.TypeSystem;
using TwinCAT.JsonExtension;
using TwinCatAdsTool.Interfaces.Services;

namespace TwinCatAdsTool.Logic.Services
{
    public class PersistentVariableService : IPersistentVariableService
    {
        public async Task<JObject> ReadPersistentVariables(TcAdsClient client)
        {
            var jobj = new JObject();
            if (client.IsConnected)
            {
                var loader = SymbolLoaderFactory.Create(client, new SymbolLoaderSettings(TwinCAT.SymbolsLoadMode.VirtualTree));
                var iterator = new SymbolIterator(loader.Symbols, s => s.IsPersistent && s.InstancePath.Split('.').Length == 2 && !s.InstancePath.Contains("["));

                var variables = new Dictionary<string, List<JObject>>();
                foreach (var symbol in iterator)
                {
                    var globalName = GetParentVaribleNameFromFullPath(symbol.InstancePath);
                    if (!variables.ContainsKey(globalName))
                        variables.Add(globalName, new List<JObject>());
                    variables[globalName].Add(await client.ReadJson(symbol.InstancePath, force:true));

                }

                foreach (var element in variables)
                {
                    var uo = new JObject();
                    foreach (var p in element.Value)
                    {
                        foreach (var up in p.Properties())
                        {
                            uo.Add(up);
                        }
                    }
                    jobj.Add(element.Key, uo);

                }
            }

            return jobj;
        }

        private string GetParentVaribleNameFromFullPath(string variablePath)
        {
            var names = variablePath.Split(new[] { '.' }, StringSplitOptions.RemoveEmptyEntries);
            return names[names.Length - 2];
        }
    }
}