using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace TwinCatAdsTool.Logic.Router
{
    internal class Response
    {
        readonly UdpClient client;
        private int timeout;

        public Response(UdpClient client, int timeout = 10000)
        {
            this.client = client;
            this.timeout = timeout;
        }

        public async Task<List<ResponseResult>> ReceiveMultipleAsync()
        {
            var results = new List<ResponseResult>();
            var stopwatch = new Stopwatch();
            while (true)
            {
                stopwatch.Reset();
                stopwatch.Start();
                
                var worker = client.ReceiveAsync();
                var task = await Task.WhenAny(worker, Task.Delay(timeout));

                if (stopwatch.ElapsedMilliseconds < timeout && (task == worker))
                {
                    var udpResult = worker.Result;
                    results.Add(new ResponseResult(udpResult));
                }
                else
                {
                    client.Close();
                    break;
                }
            }

            return results;
        }
    }
}

