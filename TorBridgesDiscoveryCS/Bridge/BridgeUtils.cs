using System.Text.RegularExpressions;
using TorBridgesDiscoveryCS.Telegram;

namespace TorBridgesDiscoveryCS.Bridge
{
    internal sealed class BridgeUtils
    {

        public static Regex Obfs4SelectorRegex = new Regex(@"obfs4\s+((?:(?:\d{1,3}\.){3}\d{1,3}|\[?(?:[a-fA-F0-9:]+)\]?)):\d+\s+[A-F0-9]{40}\s+cert=\S+\s+iat-mode=[0-2]");
        
        public static Regex WebtunnelSelectorRegex = new Regex(@"webtunnel\s+(?:(?:(?:\d{1,3}\.){3}\d{1,3})|(?:\[(?:(?:[a-fA-F0-9]{1,4}:){7}[a-fA-F0-9]{1,4}|(?:(?:[a-fA-F0-9]{1,4}:)*::(?:[a-fA-F0-9]{1,4}:)*[a-fA-F0-9]{0,4}))\])):\d+\s+[A-F0-9]{40}\s+\S+\s+ver=[1-10]\.[1-10]\.[1-10]");

        public static Bridge[] SelectUniqueBridgesFromString(string lines)
        {
            List<Bridge> collectedBridges = new();
            MatchCollection obfs4BridgesMatches = BridgeUtils.Obfs4SelectorRegex.Matches(lines);
            MatchCollection webtunnelBridgesMatches = BridgeUtils.WebtunnelSelectorRegex.Matches(lines);
            foreach (Match obfs4match in obfs4BridgesMatches)
            {
                Obfs4Bridge? parsedObfs4;
                if (Obfs4Bridge.TryParse(obfs4match.Value, out parsedObfs4))
                {
                    if (!collectedBridges.Any(x => x.Equals(parsedObfs4)))
                    {
#pragma warning disable CS8604 // Possible null reference argument; if TryParse returns true, parsedObfs4 is not null
                        collectedBridges.Add(parsedObfs4);
#pragma warning restore CS8604 // Possible null reference argument.
                    }
                }
            }
            foreach (Match webtunnelmatch in webtunnelBridgesMatches)
            {
                WebtunnelBridge? parsedWebtunnel;
                if (WebtunnelBridge.TryParse(webtunnelmatch.Value, out parsedWebtunnel))
                {
                    if (!collectedBridges.Any(x => x.Equals(parsedWebtunnel)))
                    {
#pragma warning disable CS8604 // Possible null reference argument; if TryParse returns true, parsedWebtunnel is not null
                        collectedBridges.Add(parsedWebtunnel);
#pragma warning restore CS8604 // Possible null reference argument.
                    }
                }
            }
            return [.. collectedBridges];
        }

        public static Bridge[] SelectUniqueBridges(IEnumerable<Bridge> bridges)
        {
            List<Bridge> uniqueBridges = new List<Bridge>();
            foreach (Bridge bridge in bridges)
            {
                if (!uniqueBridges.Any(x => x.Equals(bridge)))
                {
                    uniqueBridges.Add(bridge);
                }
            }
            return uniqueBridges.ToArray();
        }
    }
}
