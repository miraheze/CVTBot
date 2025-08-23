using System;
using System.Net;
using System.Net.Sockets;

namespace CVTBot
{
    internal static class IPRangeUtils
    {
        public static string CIDRToIPRange(string cidr)
        {
            IPAddress ip;
            int maskBits;
            if (cidr.Contains(":")) // IPv6
            {
                var parts = cidr.Split('/');
                ip = IPAddress.Parse(parts[0]);
                maskBits = int.Parse(parts[1]);
            }
            else // IPv4
            {
                var parts = cidr.Split('/');
                ip = IPAddress.Parse(parts[0]);
                maskBits = int.Parse(parts[1]);
            }

            var mask = IPAddressExtensions.CreateSubnetMask(ip.AddressFamily, maskBits);
            var network = ip.GetNetworkAddress(mask);
            var broadcast = ip.AddressFamily == AddressFamily.InterNetwork ? GetBroadcastAddress(ip, mask) : null;
            return $"{network} - {(broadcast != null ? broadcast.ToString() : "N/A")}";
        }

        public static string IPRangeToCIDR(string start, string end)
        {
            IPAddress startIP = IPAddress.Parse(start);
            IPAddress endIP = IPAddress.Parse(end);
            var subnetMask = startIP.GetSubnetMask(endIP);
            var network = startIP.GetNetworkAddress(subnetMask);
            var maskBits = subnetMask.GetMaskBits();
            return $"{network}/{maskBits}";
        }

        public static bool IsIPInRange(string ip, string start, string end)
        {
            IPAddress startIP = IPAddress.Parse(start);
            IPAddress endIP = IPAddress.Parse(end);
            IPAddress ipToCheck = IPAddress.Parse(ip);
            if (startIP.AddressFamily != ipToCheck.AddressFamily || endIP.AddressFamily != ipToCheck.AddressFamily)
            {
                return false; // IP versions must match
            }
            var subnetMask = startIP.GetSubnetMask(endIP);
            var network = startIP.GetNetworkAddress(subnetMask);
            var startBytes = startIP.GetAddressBytes();
            var endBytes = endIP.GetAddressBytes();
            var ipBytes = ipToCheck.GetAddressBytes();
            var networkBytes = network.GetAddressBytes();
            for (int i = 0; i < startBytes.Length; i++)
            {
                startBytes[i] &= networkBytes[i];
                endBytes[i] &= networkBytes[i];
                ipBytes[i] &= networkBytes[i];
            }
            var startUInt = BitConverter.ToUInt32(startBytes, 0);
            var endUInt = BitConverter.ToUInt32(endBytes, 0);
            var ipUInt = BitConverter.ToUInt32(ipBytes, 0);
            return ipUInt >= startUInt && ipUInt <= endUInt;
        }

        private static IPAddress GetBroadcastAddress(IPAddress ipAddress, IPAddress subnetMask)
        {
            var ipBytes = ipAddress.GetAddressBytes();
            var maskBytes = subnetMask.GetAddressBytes();
            var broadcastBytes = new byte[ipBytes.Length];
            for (int i = 0; i < ipBytes.Length; i++)
            {
                broadcastBytes[i] = (byte)(ipBytes[i] & maskBytes[i]);
                broadcastBytes[i] |= (byte)~maskBytes[i];
            }
            return new IPAddress(broadcastBytes);
        }
    }

    internal static class IPAddressExtensions
    {
        public static IPAddress CreateSubnetMask(AddressFamily addressFamily, int maskBits)
        {
            var maskBytes = new byte[addressFamily == AddressFamily.InterNetwork ? 4 : 16];
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
            var networkBytes = new byte[ipBytes.Length];
            for (int i = 0; i < ipBytes.Length; i++)
            {
                networkBytes[i] = (byte)(ipBytes[i] & maskBytes[i]);
            }
            return new IPAddress(networkBytes);
        }

        public static IPAddress GetSubnetMask(this IPAddress startIP, IPAddress endIP)
        {
            var startBytes = startIP.GetAddressBytes();
            var endBytes = endIP.GetAddressBytes();
            var maskBytes = new byte[startBytes.Length];
            for (int i = 0; i < startBytes.Length; i++)
            {
                maskBytes[i] = (byte)(startBytes[i] & endBytes[i]);
            }
            return new IPAddress(maskBytes);
        }

        public static int GetMaskBits(this IPAddress subnetMask)
        {
            var maskBytes = subnetMask.GetAddressBytes();
            var maskBits = 0;
            for (int i = 0; i < maskBytes.Length; i++)
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
