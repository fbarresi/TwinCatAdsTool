using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TwinCatAdsTool.Logic.Router
{
    internal static class Segment
    {
        public static readonly byte[] HEADER = { 0x03, 0x66, 0x14, 0x71 };
        public static readonly byte[] END = { 0, 0, 0, 0 };
        public static readonly byte[] AMSNETID = { 0, 0, 0, 0, 1, 1 };
        public static readonly byte[] PORT = { 0x10, 0x27 };

        public static readonly byte[] REQUEST_ADDROUTE = { 6, 0, 0, 0 };
        public static readonly byte[] REQUEST_DISCOVER = { 1, 0, 0, 0 };
        public static readonly byte[] ROUTETYPE_TEMP = { 6, 0, 0, 0 };
        public static readonly byte[] ROUTETYPE_STATIC = { 5, 0, 0, 0 };
        public static readonly byte[] TEMPROUTE_TAIL = { 9, 0, 4, 0, 1, 0, 0, 0 };
        public static readonly byte[] ROUTENAME_L = { 0x0c, 0, 0, 0 };
        public static readonly byte[] USERNAME_L = { 0x0d, 0, 0, 0 };
        public static readonly byte[] PASSWORD_L = { 2, 0, 0, 0 };
        public static readonly byte[] LOCALHOST_L = { 5, 0, 0, 0 };
        public static readonly byte[] AMSNETID_L = { 7, 0, 6, 0 };

        public static readonly byte[] RESPONSE_ADDROUTE = { 6, 0, 0, 0x80 };
        public static readonly byte[] RESPONSE_DISCOVER = { 1, 0, 0, 0x80 };
        public static readonly byte[] TCATTYPE_ENGINEERING = { 4, 0, 0x94, 0, 0x94, 0, 0, 0 };
        public static readonly byte[] TCATTYPE_RUNTIME = { 4, 0, 0x14, 1, 0x14, 1, 0, 0 };

        public static readonly int L_NAMELENGTH = 4;
        public static readonly int L_OSVERSION = 12;
        public static readonly int L_DESCRIPTIONMARKER = 4;
        public static readonly int L_ROUTEACK = 4;
    }
}
