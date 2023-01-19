using System;
using System.Net;

namespace CVTBot
{
    internal static class CIDRChecker
    {
        public static bool IsValidCIDR(string cidr)
        {
            try
            {
                string[] parts = cidr.Split('/');
                IPAddress ip = IPAddress.Parse(parts[0]);
                if (ip.AddressFamily == ip.AddressFamily.InterNetwork)
                {
                    byte prefix = byte.Parse(parts[1]);
                    if (prefix >= 0 && prefix <= 32)
                    {
                        return true;
                    }
                }
                else if (ip.AddressFamily == ip.AddressFamily.InterNetworkV6)
                {
                    byte prefix = byte.Parse(parts[1]);
                    if (prefix >= 0 && prefix <= 128)
                    {
                        return true;
                    }
                }
            }
            catch (Exception)
            {
                return false;
            }
            return false;
        }

        public static bool IsIPInCIDR(string ip, string cidr)
        {
            try
            {
                IPAddress ipAddress = IPAddress.Parse(ip);
                if (ipAddress.AddressFamily == ipAddress.AddressFamily.InterNetwork)
                {
                    IPAddress cidrAddress = IPAddress.Parse(cidr.Split('/')[0]);
                    byte cidrPrefix = byte.Parse(cidr.Split('/')[1]);

                    byte[] ipAddressBytes = ipAddress.GetAddressBytes();
                    byte[] cidrAddressBytes = cidrAddress.GetAddressBytes();

                    for (int i = 0; i < cidrPrefix / 8; i++)
                    {
                        if (ipAddressBytes[i] != cidrAddressBytes[i])
                        {
                            return false;
                        }
                    }

                    int shift = cidrPrefix % 8;
                    if (shift != 0)
                    {
                        byte mask = (byte)(0xff << (8 - shift));
                        if ((ipAddressBytes[cidrPrefix / 8] & mask) != (cidrAddressBytes[cidrPrefix / 8] & mask))
                        {
                            return false;
                        }
                    }

                    return true;
                }
                else if(ipAddress.AddressFamily == ipAddress.AddressFamily.InterNetworkV6)
                {
                    var cidrParts = cidr.Split('/');
                    var ipAddressBytes = ipAddress.GetAddressBytes();
                    var cidrAddressBytes = IPAddress.Parse(cidrParts[0]).GetAddressBytes();
                    var cidrPrefix = int.Parse(cidrParts[1]);
                    var bytesToCheck = cidrPrefix / 8;
                    var bitsToCheck = cidrPrefix % 8;
                    var mask = (byte)(0xff >> bitsToCheck);
                    for (int i = 0; i < bytesToCheck; i++)
                    {
                        if (ipAddressBytes[i] != cidrAddressBytes[i])
                        {
                            return false;
                        }
                    }
                    if (bitsToCheck != 0)
                    {
                        if ((ipAddressBytes[bytesToCheck] & mask) != (cidrAddressBytes[bytesToCheck] & mask))
                        {
                            return false;
                        }
                    }
                    return true;
                }
            }
            catch (Exception)
            {
                return false;
            }
        }
    }
}
