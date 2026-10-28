using log4net;
using Meebey.SmartIrc4net;
using System;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;

namespace CVTBot
{
    /// <summary>
    /// Proxy and SASL helpers shared by the main and the reader IRC clients.
    /// </summary>
    internal static class IrcSupport
    {
        private static readonly ILog logger = LogManager.GetLogger("CVTBot.IrcSupport");

        /// <summary>
        /// Routes the client through a proxy given as a URL. An empty value keeps the connection direct.
        /// </summary>
        public static void ConfigureProxy(IrcClient client, string proxy)
        {
            if (string.IsNullOrWhiteSpace(proxy))
            {
                return;
            }

            // The messages below leave the proxy out on purpose. It can hold credentials.
            if (!Uri.TryCreate(proxy.Trim(), UriKind.Absolute, out Uri uri) || string.IsNullOrEmpty(uri.Host))
            {
                throw new FormatException("Invalid IRC proxy. Expected a URL like socks5://127.0.0.1:1080.");
            }

            ProxyType type = uri.Scheme.ToLowerInvariant() switch
            {
                "http" => ProxyType.Http,
                "socks4" => ProxyType.Socks4,
                "socks4a" => ProxyType.Socks4a,
                "socks5" => ProxyType.Socks5,
                _ => throw new FormatException($"Unsupported IRC proxy scheme '{uri.Scheme}'. Use http, socks4, socks4a or socks5.")
            };

            if (uri.Port <= 0)
            {
                throw new FormatException("The IRC proxy needs a port.");
            }

            client.ProxyType = type;
            client.ProxyHost = uri.Host;
            client.ProxyPort = uri.Port;

            if (!string.IsNullOrEmpty(uri.UserInfo))
            {
                string[] credentials = uri.UserInfo.Split(':', 2);
                client.ProxyUsername = Uri.UnescapeDataString(credentials[0]);
                client.ProxyPassword = credentials.Length > 1 ? Uri.UnescapeDataString(credentials[1]) : string.Empty;
            }
        }

        /// <summary>
        /// Connects the client. The host is resolved here only when the proxy cannot do it.
        /// </summary>
        public static void Connect(IrcClient client, string server, int port)
        {
            switch (client.ProxyType)
            {
                case ProxyType.None:
                    // Direct connections go over IPv6.
                    client.Connect(Resolve(server, AddressFamily.InterNetworkV6), port);
                    break;
                case ProxyType.Socks4:
                    // SOCKS4 only takes an IPv4 address.
                    client.Connect(Resolve(server, AddressFamily.InterNetwork), port);
                    break;
                default:
                    // The proxy resolves the hostname. Local DNS stays out of it.
                    client.Connect(server, port);
                    break;
            }
        }

        private static string Resolve(string server, AddressFamily family)
        {
            return Dns.GetHostAddresses(server).First(a => a.AddressFamily == family).ToString();
        }

        /// <summary>
        /// Authenticates with SASL PLAIN on every connect. Login() must be given a null password
        /// for this client, so that no server password is sent as well.
        /// </summary>
        public static void EnableSasl(IrcClient client, string account, string password)
        {
            // CAP REQ goes out before NICK and USER. Registration waits for CAP END.
            client.OnConnected += (sender, e) => client.WriteLine("CAP REQ :sasl", Priority.Critical);
            client.OnReadLine += (sender, e) => HandleLine(client, account, password, e.Line);
        }

        private static void HandleLine(IrcClient client, string account, string password, string line)
        {
            // Drop any message tags and the prefix, then split into command and parameters
            if (line.StartsWith('@'))
            {
                int tagEnd = line.IndexOf(' ');
                if (tagEnd < 0)
                {
                    return;
                }

                line = line[(tagEnd + 1)..];
            }

            if (line.StartsWith(':'))
            {
                int prefixEnd = line.IndexOf(' ');
                if (prefixEnd < 0)
                {
                    return;
                }

                line = line[(prefixEnd + 1)..];
            }

            string[] parts = line.Split(' ', 4);

            try
            {
                if (parts[0] == "CAP" && parts.Length == 4 && parts[2] == "ACK" && parts[3].Contains("sasl"))
                {
                    client.WriteLine("AUTHENTICATE PLAIN", Priority.Critical);
                }
                else if (parts[0] == "CAP" && parts.Length >= 3 && parts[2] == "NAK")
                {
                    logger.WarnFormat("IRC server refused SASL for {0}. Continuing without it.", account);
                    client.WriteLine("CAP END", Priority.Critical);
                }
                else if (parts[0] == "AUTHENTICATE" && parts.Length >= 2 && parts[1] == "+")
                {
                    SendCredentials(client, account, password);
                }
                else if (parts[0] == "903")
                {
                    logger.InfoFormat("Authenticated as {0} with SASL", account);
                    client.WriteLine("CAP END", Priority.Critical);
                }
                else if (parts[0] is "904" or "905" or "906" or "907")
                {
                    // 907 means already authenticated. The rest are failures.
                    if (parts[0] != "907")
                    {
                        logger.WarnFormat("SASL authentication failed for {0} ({1}). Continuing without it.", account, parts[0]);
                    }

                    client.WriteLine("CAP END", Priority.Critical);
                }
            }
            catch (NotConnectedException)
            {
                // The connection dropped mid handshake. The reconnect logic starts it over.
            }
        }

        private static void SendCredentials(IrcClient client, string account, string password)
        {
            // PLAIN is an empty authzid, the account name and the password, split by NUL bytes
            string payload = Convert.ToBase64String(Encoding.UTF8.GetBytes($"\0{account}\0{password}"));

            // Long payloads go out in 400 byte pieces, with a lone + if the last piece is full
            for (int i = 0; i < payload.Length; i += 400)
            {
                client.WriteLine($"AUTHENTICATE {payload.Substring(i, Math.Min(400, payload.Length - i))}", Priority.Critical);
            }

            if (payload.Length % 400 == 0)
            {
                client.WriteLine("AUTHENTICATE +", Priority.Critical);
            }
        }
    }
}
