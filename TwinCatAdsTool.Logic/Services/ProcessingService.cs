using System;
using System.IO;
using System.Linq;
using System.Reactive;
using System.Threading.Tasks;
using System.Windows.Forms;
using Newtonsoft.Json;
using TwinCatAdsTool.Interfaces.Services;

namespace TwinCatAdsTool.Logic.Services
{
    public class ProcessingService : IProcessingService
    {
        private readonly IClientService clientService;
        private readonly IPersistentVariableService persistentVariableService;

        public ProcessingService(IClientService clientService, IPersistentVariableService persistentVariableService)
        {
            this.clientService = clientService;
            this.persistentVariableService = persistentVariableService;
        }

        public Task Connect(string amsId, int port)
        {
            return clientService.Connect(amsId, port);
        }


        public async Task Process(string fileName)
        {
            if (string.IsNullOrEmpty(fileName))
            {
                return;
            }

            var result = await persistentVariableService.ReadPersistentVariables(clientService.Client);

            using (StreamWriter file = File.CreateText(fileName))
            using (JsonTextWriter writer = new JsonTextWriter(file))
            {
                writer.Formatting = Formatting.Indented;
                result.WriteTo(writer);
            }
        }
    }
}