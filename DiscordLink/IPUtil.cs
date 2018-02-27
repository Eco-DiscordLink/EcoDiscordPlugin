using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;

namespace Eco.Plugins.DiscordLink
{
    public class IPUtil
    {
        public static List<IPAddress> GetInterNetworkIP()
        {
            List<IPAddress> interNetworkIps = new List<IPAddress>();
            IPAddress[] localIPs = Dns.GetHostAddresses(Dns.GetHostName());
            foreach (IPAddress addr in localIPs)
            {
                if (addr.AddressFamily == AddressFamily.InterNetwork)
                {
                    interNetworkIps.Add(addr);
                }
            }

            return interNetworkIps;
        }
    }
}