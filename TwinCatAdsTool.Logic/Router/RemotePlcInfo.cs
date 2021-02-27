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
        public string Name { get; set; } = "";

        public IPAddress Address { get; set; } = IPAddress.Any;

        public AmsNetId AmsNetId { get; set; } = new AmsNetId("127.0.0.1.1.1");


        public string OsVersion { get; set; } = "";

        public string Comment { get; set; } = "";

        public AdsVersion TcVersion  = new AdsVersion(3,1,4024);


        public bool IsRuntime { get; set; } = false;

        public string TcVersionString { get { return TcVersion.Version.ToString() + "." + TcVersion.Revision.ToString() + "." + TcVersion.Build.ToString(); } }
    }
}
