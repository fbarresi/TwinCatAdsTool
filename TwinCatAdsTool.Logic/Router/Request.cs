using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace TwinCatAdsTool.Logic.Router
{
    internal class Request
    {
        public const int DEFAULT_UDP_PORT = 48899;

        readonly UdpClient client;
        public UdpClient Client { get { return client; } }

        public int timeout;
        public int Timeout
        {
            get { return timeout; }

            set
            {
                timeout = value;
                client.Client.ReceiveTimeout = client.Client.SendTimeout = Timeout;
            }
        }

        public Request(int timeout = 10000)
        {
            client = new UdpClient();
            client.EnableBroadcast = true;
            client.Client.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);

            Timeout = timeout;
        }

        public async Task<Response> SendAsync(IPEndPoint endPoint)
        {
            byte[] data = GetRequestBytes;
            await client.SendAsync(data, data.Length, endPoint);

            return new Response(client, Timeout);
        }

        readonly List<byte[]> listOfBytes = new List<byte[]>();
        public byte[] GetRequestBytes
        {
            get { return listOfBytes.SelectMany(a => a).ToArray(); }
        }

        public void Add(byte[] segment)
        {
            listOfBytes.Add(segment);
        }

        public void Clear()
        {
            listOfBytes.Clear();
        }
    }
}
