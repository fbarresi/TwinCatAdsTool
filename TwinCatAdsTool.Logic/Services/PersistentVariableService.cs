using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Threading.Tasks;
using log4net;
using log4net.Core;
using Newtonsoft.Json.Linq;
using TwinCAT;
using TwinCAT.Ads;
using TwinCAT.Ads.TypeSystem;
using TwinCAT.JsonExtension;
using TwinCAT.TypeSystem;
using TwinCAT.TypeSystem.Generic;
using TwinCatAdsTool.Interfaces.Logging;
using TwinCatAdsTool.Interfaces.Services;
using TwinCatAdsTool.Logic.Properties;

namespace TwinCatAdsTool.Logic.Services
{
    public class PersistentVariableService : IPersistentVariableService
    {
        private readonly ILog logger =LoggerFactory.GetLogger();
        private readonly Subject<string> currentTaskSubject = new Subject<string>();
        public async Task<JObject> ReadGlobalPersistentVariables(AdsClient client, IInstanceCollection<ISymbol> symbols)
        {
            var jobj = new JObject();
            try
            {
                if (client.IsConnected)
                {
                    var iterator = new SymbolIterator(symbols,
                            s => s.IsPersistent && s.InstancePath.Split('.').Length >= 2 &&
                                 !s.InstancePath.Contains("["))
                        ;

                    var persistentSymbols = iterator.Where(s => s.Parent != null ? !iterator.Contains(s.Parent) : true);

                    var variables = new Dictionary<string, List<JObject>>();
                    foreach (var symbol in persistentSymbols)
                    {
                        var splitPath = symbol.InstancePath.Split('.');
                        var localName = splitPath.Last();
                        var globalName = symbol.InstancePath.Replace($".{localName}", string.Empty);
                        
                        if (!variables.ContainsKey(globalName))
                        {
                            variables.Add(globalName, new List<JObject>());
                        }

                        try
                        {
                            logger.Debug($"reading symbol '{symbol.InstancePath}' in json format...");
                            currentTaskSubject.OnNext($"Reading {symbol.InstancePath}...");

                            var json = await client.ReadJson(symbol.InstancePath, force:true);
                            if(json.ContainsKey(localName))
                                variables[globalName].Add(json);
                            else
                            {
                                var innerObject = new JObject();
                                innerObject.Add(localName, json);
                                variables[globalName].Add(innerObject);
                            }
                        }
                        catch (Exception e)
                        {
                            logger.Error(string.Format(Resources.ErrorDuringReadingVariable0InJsonFormat, symbol.InstancePath), e);
                        }

                    }

                    foreach (var element in variables.OrderBy(pair => pair.Key.Length))
                    {
                        var uo = new JObject();
                        foreach (var p in element.Value)
                        {
                            foreach (var up in p.Properties())
                            {
                                uo.Add(up);
                            }
                        }
                        var path = element.Key.Split(".");
                        if (path.Count() == 1)
                        {
                            jobj.Add(element.Key, uo);
                        }
                        else
                        {
                            for(var i = 0; i < path.Length-1; i++)
                            {
                                if ((jobj.SelectToken(string.Join(".", path.Take(i+1))) as JObject) == null)
                                {
                                    if (i > 0)
                                    {
                                        (jobj.SelectToken(string.Join(".", path.Take(i))) as JObject).Add(path[i], new JObject());
                                    }
                                    else
                                    {
                                        jobj.Add(path[i], new JObject());
                                    }
                                }
                            }

                            try
                            {
                                (jobj.SelectToken(string.Join(".", path.Take(path.Length - 1))) as JObject).Add(path.Last(), uo);
                            }
                            catch(ArgumentException)
                            {
                                var token = (jobj.SelectToken(string.Join(".", path)) as JObject);
                                if(token != null)
                                {
                                    foreach (var prop in uo.Properties())
                                    {
                                        token.Add(prop.Name, prop.Value);
                                    }
                                }
                            }
                        }
	
                    }

                }
            }
            catch (Exception e)
            {
                logger.Error(Resources.ErrorWhileReadingPersistentVariables,e);
            }
            
            currentTaskSubject.OnNext(string.Empty);
            logger.Debug($"Persistent variable successfully downloaded!");

            return jobj;
        }

        public IObservable<string> CurrentTask => currentTaskSubject.AsObservable();
    }
}