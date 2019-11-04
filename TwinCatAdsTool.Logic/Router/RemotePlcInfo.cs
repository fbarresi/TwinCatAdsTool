using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using TwinCAT.Ads;

namespace TwinCatAdsTool.Logic.Router
{
    public class RemotePlcInfo
    {
        public string Name = "";
        public IPAddress Address = IPAddress.Any;
        public AmsNetId AmsNetId = new AmsNetId("127.0.0.1.1.1");
        public string OsVersion = "";
        public string Comment = "";
        public AdsVersion TcVersion = new AdsVersion();
        public bool IsRuntime = false;

        public string TcVersionString { get { return TcVersion.Version.ToString() + "." + TcVersion.Revision.ToString() + "." + TcVersion.Build.ToString(); } }
    }
}
