using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TwinCatAdsTool.Interfaces.Services
{
    public interface IProcessingService
    {
        Task Connect(string amsId, int port);
        Task Process(string fileName);
    }
}
