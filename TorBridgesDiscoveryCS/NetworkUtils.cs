using System.Net;
using System.Net.Http;

namespace TorBridgesDiscoveryCS
{
    internal sealed class NetworkUtils
    {

        public static HttpClient SharedHttpClient { get; private set; } = new HttpClient(
            new SocketsHttpHandler()
            {
                PooledConnectionLifetime = TimeSpan.FromMinutes(1)
            }
        )
        {
            Timeout = TimeSpan.FromMinutes(1),
        };

        public static string GetAsString(string url)
        {
            return SharedHttpClient.Send(new HttpRequestMessage(HttpMethod.Get, url)).Content.ReadAsStringAsync().Result;
        }

        public static void ReinitSharedHttpClient(string? proxy = null)
        {
            SharedHttpClient.Dispose();
            if (proxy == null)
            {
                SharedHttpClient = new HttpClient(
                    new SocketsHttpHandler()
                    {
                        PooledConnectionLifetime = TimeSpan.FromMinutes(1)
                    }
                )
                {
                    Timeout = SharedHttpClient.Timeout,
                };
                return;
            }
            Uri uri = new(proxy);
            string[] creds = uri.UserInfo.Split(':', 2);
            WebProxy webProxy = new(uri);
            if (proxy.Contains('@'))
            {
                webProxy.Credentials = new NetworkCredential(creds[0], creds[1]);
                webProxy.UseDefaultCredentials = false;
            }
            SocketsHttpHandler handler = new()
            {
                PooledConnectionLifetime = TimeSpan.FromMinutes(1),
                Proxy = webProxy,
                UseProxy = true,
            };
            SharedHttpClient = new HttpClient(handler)
            {
                Timeout = SharedHttpClient.Timeout,
            };
        }
    }
}
