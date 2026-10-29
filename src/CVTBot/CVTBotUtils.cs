using System;
using System.Collections;
using System.Net;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Web;

namespace CVTBot
{
    internal static class CVTBotUtils
    {
        private static readonly Regex rStripper = new Regex(@"(,|and)");
        private static readonly Regex rSpaces = new Regex(@"\s{2,}");
        private static readonly Regex rfindValues = new Regex(@"(\d+) (year|month|fortnight|week|day|hour|minute|min|second|sec)s?");
        // TODO: Something is still wrong here, some exprs show up as 3 instead of 3 day(s)

        /// <summary>
        /// Like PHP's str_split() function, splits a string into an array of chunks
        /// </summary>
        /// <param name="input">String to split</param>
        /// <param name="chunkLen">Maximum length of each chunk</param>
        /// <returns></returns>
        public static ArrayList StringSplit(string input, int chunkLen)
        {
            ArrayList output = new ArrayList();

            // If input is already shorter than chunkLen, then...
            if (input.Length <= chunkLen)
            {
                _ = output.Add(input);
            }
            else
            {
                for (int i = 0; i < input.Length; i += chunkLen)
                {
                    int getLen = i + chunkLen > input.Length ? input.Length - i : chunkLen;
                    _ = output.Add(input.Substring(i, getLen));
                }
            }

            return output;
        }

        /// <summary>
        /// Like PHP's strtotime() function, attempts to parse a GNU date/time into number of seconds
        /// </summary>
        /// <param name="input">String representation of date/time length</param>
        /// <returns></returns>
        public static int ParseDateTimeLength(string input, int defaultLen)
        {
            string parseStr = input.ToLower();
            parseStr = rStripper.Replace(parseStr, "");
            parseStr = rSpaces.Replace(parseStr, " ");

            // Handle specials here
            switch (parseStr)
            {
                case "indefinite":
                case "infinite":
                    return 0;
                case "tomorrow":
                    return 24 * 3600;
            }

            // Now for some real parsing
            double sumSeconds = 0;
            MatchCollection mc = rfindValues.Matches(parseStr);

            foreach (Match m in mc)
            {
                string unit = m.Groups[2].Captures[0].Value;
                string value = m.Groups[1].Captures[0].Value;
                switch (unit)
                {
                    case "year":
                        sumSeconds += Convert.ToInt32(value) * 8760 * 3600; // 365 days
                        break;
                    case "month":
                        sumSeconds += Convert.ToInt32(value) * 732 * 3600; // 30.5 days
                        break;
                    case "fortnight":
                        sumSeconds += Convert.ToInt32(value) * 336 * 3600; // 14 days
                        break;
                    case "week":
                        sumSeconds += Convert.ToInt32(value) * 168 * 3600; // 7 days
                        break;
                    case "day":
                        sumSeconds += Convert.ToInt32(value) * 24 * 3600; // 24 hours
                        break;
                    case "hour":
                        sumSeconds += Convert.ToInt32(value) * 3600; // 1 hour
                        break;
                    case "minute":
                    case "min":
                        sumSeconds += Convert.ToInt32(value) * 60; // 60 seconds
                        break;
                    case "second":
                    case "sec":
                        sumSeconds += Convert.ToInt32(value); // One second
                        break;
                }
            }

            // Round the double
            int seconds = Convert.ToInt32(sumSeconds);

            return seconds == 0 ? defaultLen : seconds;
        }

        private static WebProxy httpProxy;

        /// <summary>
        /// Routes HTTP requests through a proxy given as a URL. An empty value keeps them direct.
        /// </summary>
        public static void ConfigureHttpProxy(string proxy)
        {
            httpProxy = null;

            if (string.IsNullOrWhiteSpace(proxy))
            {
                return;
            }

            // The messages below leave the proxy out on purpose. It can hold credentials.
            if (!Uri.TryCreate(proxy.Trim(), UriKind.Absolute, out Uri uri) || string.IsNullOrEmpty(uri.Host))
            {
                throw new FormatException("Invalid HTTP proxy. Expected a URL like http://127.0.0.1:8080.");
            }

            if (uri.Scheme is not ("http" or "socks4" or "socks4a" or "socks5"))
            {
                throw new FormatException($"Unsupported HTTP proxy scheme '{uri.Scheme}'. Use http, socks4, socks4a or socks5.");
            }

            if (uri.Port <= 0)
            {
                throw new FormatException("The HTTP proxy needs a port.");
            }

            // The address leaves the credentials out. They go in separately below.
            WebProxy webProxy = new WebProxy(new Uri(uri.GetComponents(UriComponents.SchemeAndServer, UriFormat.UriEscaped)));

            if (!string.IsNullOrEmpty(uri.UserInfo))
            {
                string[] credentials = uri.UserInfo.Split(':', 2);
                webProxy.Credentials = new NetworkCredential(
                    Uri.UnescapeDataString(credentials[0]),
                    credentials.Length > 1 ? Uri.UnescapeDataString(credentials[1]) : string.Empty);
            }

            httpProxy = webProxy;
        }

        /// <summary>
        /// Gets the raw source code for a URL
        /// </summary>
        /// <param name="url">Location of the resource</param>
        /// <returns></returns>
        public static string GetRawDocument(string url)
        {
            // Without a configured proxy the handler keeps its default, which reads the environment
            HttpClientHandler handler = new HttpClientHandler();
            if (httpProxy != null)
            {
                handler.Proxy = httpProxy;
            }

            using HttpClient client = new HttpClient(handler);

            client.DefaultRequestHeaders.UserAgent.ParseAdd(
                "Mozilla/5.0 (en-US) CVTBot/1.0 (+https://github.com/miraheze/CVTBot)"
            );

            try
            {
                return client.GetStringAsync(url).GetAwaiter().GetResult();
            }
            catch (HttpRequestException e)
            {
                throw new Exception("Unable to retrieve " + url + " from server. Error was: " + e.Message);
            }
        }

        /// <summary>
        /// Replaces up to the maximum number of old characters with new characters in a string
        /// </summary>
        /// <param name="input">The string to work on</param>
        /// <param name="oldChar">The character to replace</param>
        /// <param name="newChar">The character to insert</param>
        /// <param name="maxChars">The maximum number of instances to replace</param>
        /// <returns></returns>
        public static string ReplaceStrMax(string input, char oldChar, char newChar, int maxChars)
        {
            for (int i = 1; i <= maxChars; i++)
            {
                // Find first oldChar
                int place = input.IndexOf(oldChar);
                if (place == -1)
                {
                    // If not found then finish
                    break;
                }
                // Replace first oldChar with newChar
                input = input.Substring(0, place) + newChar.ToString() + input.Substring(place + 1);
            }
            return input;
        }

        /// <summary>
        /// Encodes a string for use with wiki URLs
        /// </summary>
        /// <param name="input"></param>
        /// <returns></returns>
        public static string WikiEncode(string input)
        {
            return HttpUtility.UrlEncode(input.Replace(' ', '_')).Replace("(", "%28").Replace(")", "%29").Replace("!", "%21");
        }

        /// <summary>
        /// Gets the root URL from a project name
        /// </summary>
        /// <param name="projectName">Name of the project (e.g., metawiki) to add</param>
        /// <returns></returns>
        public static string GetRootUrl(string projectName)
        {
            string subdomain = Regex.Replace(projectName, Program.config.projectSuffix + "$", "");
            string domain = Program.config.projectDomain;
            return "https://" + subdomain + "." + domain + "/";
        }
    }
}
