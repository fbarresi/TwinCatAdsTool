using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace TwinCatAdsTool.Logic.Services
{
    public static class IPHelper
    {
        public static IPAddress GetBroadcastAddress(IPAddress localhost)
        {
            IPAddress hostMask = GetHostMask(localhost);

            if (hostMask == null || localhost == null)
                return null;

            byte[] complementedMaskBytes = new byte[4];
            byte[] broadcastIpBytes = new byte[4];

            for (int i = 0; i < 4; i++)
            {
                complementedMaskBytes[i] = (byte)
                    ~(hostMask.GetAddressBytes().ElementAt(i));

                broadcastIpBytes[i] = (byte)(
                    localhost.GetAddressBytes().ElementAt(i) |
                    complementedMaskBytes[i]);
            }

            return new IPAddress(broadcastIpBytes);
        }

        /// <summary>
        /// Host mask for the given localhost address.
        /// </summary>
        /// <param name="localhost">Address of the localhost.</param>
        /// <returns>May produce exception or return null!</returns>
        public static IPAddress GetHostMask(IPAddress localhost)
        {
            string strLocalAddress = localhost.ToString();
            NetworkInterface[] interfaces = NetworkInterface.GetAllNetworkInterfaces();

            foreach (NetworkInterface netInterface in interfaces)
            {
                UnicastIPAddressInformationCollection unicastInfos = netInterface.GetIPProperties().UnicastAddresses;

                foreach (UnicastIPAddressInformation info in unicastInfos)
                    if (info.Address.ToString() == strLocalAddress)
                        return info.IPv4Mask;
            }

            return null;
        }

        public static List<IPAddress> Localhosts { get { return FilteredLocalhosts(null); } }

        public static List<IPAddress> FilteredLocalhosts(List<NetworkInterfaceType> niTypes = null)
        {
            if (niTypes == null)
                niTypes =
                    new List<NetworkInterfaceType>
                    {
                        NetworkInterfaceType.Wireless80211,
                        NetworkInterfaceType.Ethernet
                    };

            List<IPAddress> localhosts = new List<IPAddress>();

            foreach (NetworkInterface ni in NetworkInterface.GetAllNetworkInterfaces())
                if (niTypes.Contains(ni.NetworkInterfaceType))
                    foreach (UnicastIPAddressInformation unicastInfo in ni.GetIPProperties().UnicastAddresses)
                        if (unicastInfo.Address.AddressFamily == AddressFamily.InterNetwork)
                            localhosts.Add(unicastInfo.Address);

            return localhosts;
        } // FilteredLocalhosts(...)
    } // class
}
