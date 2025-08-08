using System.Net;
using System.Net.Sockets;

namespace TorBridgesDiscoveryCS.Bridge
{
    internal interface Bridge
    {

        public abstract string ToString();

        public abstract bool Equals(object? obj);

        public abstract IPEndPoint GetIpEndPoint();

    }

    internal class Obfs4Bridge : Bridge
    {

        public readonly IPEndPoint IpEndPoint;

        public readonly string Fingerprint;

        public readonly string Cert;

        public readonly uint IATMode;

        private string? _bridgeLine;

        public Obfs4Bridge(IPEndPoint endpoint, string fingerprint, string cert, uint iatMode)
        {
            if (endpoint.AddressFamily != AddressFamily.InterNetwork && endpoint.AddressFamily != AddressFamily.InterNetworkV6)
            {
                throw new ArgumentException("IP endpoint should be either ipv4 or ipv6");
            }
            if (fingerprint.Length != 40)
            {
                throw new ArgumentException("Fingerprint should always be 40 symbols long");
            }
            if (cert.Length != 70)
            {
                throw new ArgumentException("Cert should always be 70 symbols long");
            }
            if (iatMode != 0 && iatMode != 1 && iatMode != 2)
            {
                throw new ArgumentException("IAT mode should always be 0, 1 or 2");
            }
            IpEndPoint = endpoint;
            Fingerprint = fingerprint;
            Cert = cert;
            IATMode = iatMode;
        }

        public override string ToString()
        {
            _bridgeLine ??= "obfs4 " + IpEndPoint.ToString() + " " + Fingerprint + " cert=" + Cert + " iat-mode=" + IATMode.ToString();
            return _bridgeLine;
        }

        public override bool Equals(object? obj)
        {
            if (obj is Obfs4Bridge bridge)
            {
                return bridge.Fingerprint.Equals(Fingerprint) || bridge.IpEndPoint.Equals(IpEndPoint);
            }
            return false;
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(IpEndPoint, Fingerprint, Cert, IATMode);
        }

        public static Obfs4Bridge Parse(string line)
        {
            line = line.Trim();
            string[] parts = line.Split();
            if (parts[0] != "obfs4")
            {
                throw new ArgumentException("Bridge line is not an obfs4 bridge");
            }
            return new Obfs4Bridge(IPEndPoint.Parse(parts[1]), parts[2], parts[3].Remove(0, 5), uint.Parse(parts[4].Remove(0, 9)));
        }

        public static bool TryParse(string line, out Obfs4Bridge? outBridge)
        {
            try
            {
                Obfs4Bridge bridge = Parse(line);
                outBridge = bridge;
                return true;
            }
            catch (Exception)
            {
                outBridge = null;
                return false;
            }
        }

        public IPEndPoint GetIpEndPoint()
        {
            return IpEndPoint;
        }
    }

    internal class WebtunnelBridge : Bridge
    {

        public readonly IPEndPoint IpEndPoint;

        public readonly string Fingerprint;

        public readonly Uri URL;

        public readonly string Version;

        public readonly string? Servername;

        private string? _bridgeLine;

        private WebtunnelBridge(IPEndPoint endpoint, string fingerprint, string url, string ver, string? servername = null)
        {
            if (endpoint.AddressFamily != AddressFamily.InterNetwork && endpoint.AddressFamily != AddressFamily.InterNetworkV6)
            {
                throw new ArgumentException("IP endpoint should be either ipv4 or ipv6");
            }
            if (fingerprint.Length != 40)
            {
                throw new ArgumentException("Fingerprint should always be 40 symbols long");
            }
            Uri? uriResult;
            if (!(Uri.TryCreate(url, UriKind.Absolute, out uriResult) && (uriResult.Scheme == Uri.UriSchemeHttp || uriResult.Scheme == Uri.UriSchemeHttps)))
            {
                throw new ArgumentException("Invalid URL");
            }
            URL = uriResult;
            IpEndPoint = endpoint;
            Fingerprint = fingerprint;
            Version = ver;
            Servername = servername;
        }

        public override string ToString()
        {
            _bridgeLine ??= "webtunnel " + IpEndPoint.ToString() + " " + Fingerprint + " url=" + URL.ToString() + (Servername != null ? " servername=" + Servername : string.Empty);
            return _bridgeLine;
        }

        public override bool Equals(object? obj)
        {
            if (obj is WebtunnelBridge bridge)
            {
                return bridge.Fingerprint.Equals(Fingerprint) || bridge.URL.Equals(URL);
            }
            return false;
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(IpEndPoint, Fingerprint, URL, Version, Servername);
        }

        public static WebtunnelBridge Parse(string line)
        {
            line = line.Trim();
            string[] parts = line.Split();
            if (parts[0] != "webtunnel")
            {
                throw new ArgumentException("Bridge line is not a webtunnel bridge");
            }
            List<string> partsList = parts.ToList();
            string? url = partsList.Find(x => x.StartsWith("url="));
            if (url == null)
            {
                throw new ArgumentException("No URL found");
            }
            string? version = partsList.Find(x => x.StartsWith("ver="));
            if (version == null) 
            {
                throw new ArgumentException("No version found");
            }
            string? servername = partsList.Find(x => x.StartsWith("servername="));
            return new WebtunnelBridge(IPEndPoint.Parse(parts[1]), parts[2], url.Remove(0, 4), version.Remove(0, 4), servername?.Remove(0, 11));
        }

        public static bool TryParse(string line, out WebtunnelBridge? outBridge)
        {
            try
            {
                WebtunnelBridge bridge = Parse(line);
                outBridge = bridge;
                return true;
            }
            catch (Exception)
            {
                outBridge = null;
                return false;
            }
        }

        public IPEndPoint GetIpEndPoint()
        {
            return IpEndPoint;
        }
    }
}
