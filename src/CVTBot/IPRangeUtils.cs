using System;
using System.Net;

namespace CVTBot
{
    internal static class IPConverter
    {
        public static string CIDRToIPRange(string cidr)
        {
            var parts = cidr.Split('/');
            var ip = IPAddress.Parse(parts[0]);
            var maskBits = int.Parse(parts[1]);
            var mask = IPAddressExtensions.CreateSubnetMask(maskBits);
            var network = ip.GetNetworkAddress(mask);
            var broadcast = ip.GetBroadcastAddress(mask);
            return $"{network} - {broadcast}";
        }

        public static string IPRangeToCIDR(string start, string end)
        {
            var startIP = IPAddress.Parse(start);
            var endIP = IPAddress.Parse(end);
            var subnetMask = startIP.GetSubnetMask(endIP);
            var network = startIP.GetNetworkAddress(subnetMask);
            var maskBits = subnetMask.GetMaskBits();
            return $"{network}/{maskBits}";
        }

        public static bool isIPInRange(string ip, string start, string end)
        {
            var startIP = IPAddress.Parse(start);
            var endIP = IPAddress.Parse(end);
            var ipToCheck = IPAddress.Parse(ip);
            var subnetMask = startIP.GetSubnetMask(endIP);
            var network = startIP.GetNetworkAddress(subnetMask);
            return network.Equals(ipToCheck.GetNetworkAddress(subnetMask))
                && ipToCheck.CompareTo(startIP) >= 0
                && ipToCheck.CompareTo(endIP) <= 0;
        }
    }

    internal static class IPAddressExtensions
    {
        public static IPAddress CreateSubnetMask(int maskBits)
        {
            var maskBytes = new byte[4];
            for (int i = 0; i < maskBits; i++)
            {
                maskBytes[i / 8] |= (byte)(0x80 >> (i % 8));
            }
            return new IPAddress(maskBytes);
        }

        public static IPAddress GetNetworkAddress(this IPAddress ip, IPAddress subnetMask)
        {
            var ipBytes = ip.GetAddressBytes();
            var maskBytes = subnetMask.GetAddressBytes();
            var networkBytes = new byte[4];
            for (int i = 0; i < 4; i++)
            {
                networkBytes[i] = (byte)(ipBytes[i] & maskBytes[i]);
            }
            return new IPAddress(networkBytes);
        }

        public static IPAddress GetSubnetMask(this IPAddress startIP, IPAddress endIP)
        {
            var startBytes = startIP.GetAddressBytes();
            var endBytes = endIP.GetAddressBytes();
            var maskBytes = new byte[4];
            for (int i = 0; i < 4; i++)
            {
                maskBytes[i] = (byte)(startBytes[i] & endBytes[i]);
            }
            return new IPAddress(maskBytes);
        }

        public static int GetMaskBits(this IPAddress subnetMask)
        {
            var maskBytes = subnetMask.GetAddressBytes();
            var maskBits = 0;
            for (int i = 0; i < 4; i++)
            {
                maskBits += BitCount(maskBytes[i]);
            }
            return maskBits;
        }

        private static int BitCount(byte b)
        {
            int count = 0;
            for (int i = 0; i < 8; i++)
            {
                if ((b & (1 << i)) != 0)
                {
                    count++;
                }
            }
            return count;
        }
    }
}
