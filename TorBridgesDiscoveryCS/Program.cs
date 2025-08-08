using System.Runtime.InteropServices.Marshalling;
using TorBridgesDiscoveryCS.Bridge;
using TorBridgesDiscoveryCS.Telegram;

namespace TorBridgesDiscoveryCS
{
    internal class Program
    {

        public static uint Goal;

        public static List<TorBridgesDiscoveryCS.Bridge.Bridge> ReachableBridges = new List<TorBridgesDiscoveryCS.Bridge.Bridge>();

        /// <summary>
        /// Powerful tor bridges aggregation and scanning utility capable of automatic reachability scanning and pulling bridges from files, urls and public telegram channels
        /// </summary>
        /// <param name="n">The number of concurrent relays tested</param>
        /// <param name="g">Test until at least this number of working relays are found</param>
        /// <param name="timeout">Socket connection timeout in milliseconds</param>
        /// <param name="channels">Telegram channels handles (without @) separated by a semicolon</param>
        /// <param name="proxy">Set proxy for telegram posts download. Format: http://user:pass@host:port; socks5h://user:pass@host:port</param>
        /// <param name="outputCollected">File to output all relays collected from telegram channels (not checked for reachability)</param>
        /// <param name="outputReachable">File to output reachable relays</param>
        /// <param name="torrc">Write bridges to files in torrc format</param>
        /// <param name="urls">Collect additional bridges from these URLs, separated by a semicolon</param>
        /// <param name="files">Collect additional bridges from these files, separated by a semicolon</param>
        /// <param name="uniqueOnly">Select only unique bridges for scanning</param>
        /// <param name="scan">Scan bridges for reachability</param>
        public static void Main(uint n=50, uint g=3, uint timeout=750, string channels="RuTorBridgesObfs4Webtunnel", string? urls=null, string? files=null, string? proxy=null, string? outputCollected = null, string? outputReachable = null, bool torrc = false, bool uniqueOnly=true, bool scan=true)
        {
            Goal = g;
            NetworkUtils.ReinitSharedHttpClient(proxy);

            // Collecting bridges from channels
            string[] bridgeChannels = channels.Split(';');
            if ((bridgeChannels.Length == 0) || (string.IsNullOrWhiteSpace(bridgeChannels[0])))
            {
                Console.WriteLine("fail: no telegram channels setted");
                Console.WriteLine("Press any key to exit...");
                Console.ReadKey();
            }

            List<TelegramChannelBridesCollector> collectors = new(bridgeChannels.Length);
            TelegramChannelBridesCollector collector;
            Console.Title = "Collecting bridges...";
            foreach (string channel in bridgeChannels)
            {
                collector = new TelegramChannelBridesCollector(channel);
                collector.OnCollectionEnded += Collector_OnCollectionEnded;

                collectors.Add(collector);
                collector.Start();
                Console.WriteLine("done: start collecting channel {0}", channel);
            }

            TelegramChannelBridesCollector.WaitAll(collectors);
            List<TorBridgesDiscoveryCS.Bridge.Bridge> bridges = TelegramChannelBridesCollector.SumCollectedBridges(collectors).ToList();
            Console.WriteLine("done: collected total of {0} bridges from {1} telegram channel(s)", bridges.Count, collectors.Count);
            // 

            // Collecting bridges from URLs
            if (urls != null)
            {
                string pageContents;
                TorBridgesDiscoveryCS.Bridge.Bridge[] collectedUrlBridges;
                foreach (string url in urls.Split(";"))
                {
                    try
                    {
                        pageContents = NetworkUtils.GetAsString(url);
                    }
                    catch(Exception e)
                    {
                        Console.WriteLine("fail: url download {0} - {1}", url, e.Message);
                        continue;
                    }
                    collectedUrlBridges = BridgeUtils.SelectUniqueBridgesFromString(pageContents);
                    Console.WriteLine("done: collected {0} bridges from {1} url", collectedUrlBridges.Length, url);
                    bridges.AddRange(collectedUrlBridges); 
                }
            }
            //

            // Collecting bridges from files
            if (files != null)
            {
                string fileContents;
                TorBridgesDiscoveryCS.Bridge.Bridge[] collectedFileBridges;
                foreach (string file in files.Split(";"))
                {
                    try
                    {
                        fileContents = File.ReadAllText(file);
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine("fail: file read {0} - {1}", file, e.Message);
                        continue;
                    }
                    collectedFileBridges = BridgeUtils.SelectUniqueBridgesFromString(fileContents);
                    Console.WriteLine("done: collected {0} from {1} file", collectedFileBridges.Length, file);
                    bridges.AddRange(collectedFileBridges);
                }
            }
            //

            Console.WriteLine("done: collected bridges from all sources - {0}", bridges.Count);

            if (uniqueOnly)
            {
                bridges = BridgeUtils.SelectUniqueBridges(bridges).ToList();
                Console.WriteLine("done: unique bridges selected - {0}", bridges.Count);
            }

            if (outputCollected != null)
            {
                if (WriteBridgesToFile(bridges, outputCollected, torrc))
                {
                    Console.WriteLine("done: output collected relays");
                }
                else
                {
                    Console.WriteLine("fail: output collected relays");
                }
            }

            if (!scan)
            {
                Console.WriteLine("Press any key to exit...");
                Console.ReadKey();
                return;
            }

            // Scanning bridges
            BridgeScanner.StartScan(TimeSpan.FromMilliseconds(timeout), (int)n, bridges.ToArray());
            BridgeScanner.OnNewWorkingRelay += BridgeScanner_OnNewWorkingRelay;
            Console.Title = "Checking bridges...";
            Console.WriteLine("done: scan started");

            BridgeScanner.WaitForEnd();


            if (outputReachable != null)
            {
                if (WriteBridgesToFile(ReachableBridges, outputReachable, torrc))
                {
                    Console.WriteLine("done: output reachable relays");
                }
                else
                {
                    Console.WriteLine("fail: output reachable relays");
                }
            }

            Console.WriteLine("done: scan ended");
            Console.WriteLine("Press any key to exit...");
            Console.ReadKey();
        }

        private static void Collector_OnCollectionEnded(object? sender, OnCollectionEndedEventArgs e)
        {
            Console.WriteLine("done: collected {0} bridges from {1}", e.BridesCollector.CollectedBridges.Count, e.BridesCollector.TelegramChannelHandle);
        }

        private static void BridgeScanner_OnNewWorkingRelay(object? sender, OnNewWorkingRelayEventArgs e)
        {
            lock (ReachableBridges)
            {
                if (ReachableBridges.Count >= Goal)
                {
                    return;
                }
                Console.WriteLine(e.Bridge.ToString());
                ReachableBridges.Add(e.Bridge);
                Console.Title = string.Format("Checking bridges: {0} reachable", ReachableBridges.Count);
                if (ReachableBridges.Count >= Goal)
                {
                    Console.WriteLine("done: relay goals reached - {0}", ReachableBridges.Count);
                    BridgeScanner.StopScan();
                }
            }
        }

        private static bool WriteBridgesToFile(IEnumerable<TorBridgesDiscoveryCS.Bridge.Bridge> bridges, string file, bool torrcFormat)
        {
            try
            {
                if (torrcFormat)
                {
                    List<string> contents = new();
                    if (File.Exists(file))
                    {
                        contents = File.ReadAllLines(file).ToList();
                    }
                    if (!contents.Contains("UseBridges 1"))
                    {
                        contents.Remove("UseBridges 0");
                        contents.Add("UseBridges 1");
                    }
                    contents.RemoveAll(x => x.StartsWith("Bridge "));
                    contents.AddRange(bridges.ToList().ConvertAll(x => "Bridge " + x.ToString()));
                    File.WriteAllLines(file, contents);
                }
                else
                {
                    File.WriteAllLines(file, bridges.ToList().ConvertAll(x => x.ToString()));
                }
                return true;
            }
            catch(Exception e)
            {
                Console.WriteLine("fail: output bridges - {0}", e.Message);
                return false;
            }
        }
    }
}
