using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace TwinCatAdsTool.Logic.Router
{
    internal class ResponseResult
    {
        private readonly UdpReceiveResult result;
        public byte[] Buffer { get { return result.Buffer; } }
        public IPAddress RemoteHost { get { return result.RemoteEndPoint.Address; } }

        public int Shift { get; set; }

        public ResponseResult(UdpReceiveResult result)
        {
            this.result = result;
            Shift = 0;
        }

        public byte[] NextChunk(int length, bool dontShift = false, int add = 0)
        {
            var to = new byte[length];
            Array.Copy(result.Buffer, Shift, to, 0, length);

            if (!dontShift)
            {
                Shift += length + add;
            }

            return to;
        }
    }
}
