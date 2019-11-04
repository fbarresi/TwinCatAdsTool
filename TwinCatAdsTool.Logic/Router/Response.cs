using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace TwinCatAdsTool.Logic.Router
{
    internal class Response
    {
        UdpClient client;
        public UdpClient Client { get { return client; } }
        public int Timeout;

        public Response(UdpClient client, int timeout = 10000)
        {
            this.client = client;
            Timeout = timeout;
        }

        public async Task<ResponseResult> ReceiveAsync()
        {
            ResponseResult result = null;

            var worker = client.ReceiveAsync();
            var task = await Task.WhenAny(worker, Task.Delay(Timeout));

            if (task == worker)
            {
                UdpReceiveResult udpResult = await worker;
                result = new ResponseResult(udpResult);
            }
            else
            {
                client.Close();
            }

            return result;
        }

        public async Task<List<ResponseResult>> ReceiveMultipleAsync()
        {
            List<ResponseResult> results = new List<ResponseResult>();
            int start = Environment.TickCount;
            while (true)
            {
                var worker = client.ReceiveAsync();
                var task = await Task.WhenAny(worker, Task.Delay(Timeout));

                long interval = (long)TimeSpan.FromTicks(Environment.TickCount - start).TotalMilliseconds - start;
                if ((interval < Timeout) && (task == worker))
                {
                    UdpReceiveResult udpResult = await worker;
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

