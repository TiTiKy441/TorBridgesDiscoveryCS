using System.Text.RegularExpressions;
using TorBridgesDiscoveryCS.Bridge;

namespace TorBridgesDiscoveryCS.Telegram
{
    internal sealed class TelegramChannelBridesCollector
    {

        public readonly string TelegramChannelHandle;

        public bool Scanning
        {
            get
            {
                return _cancellationTokenSource != null && !_cancellationTokenSource.IsCancellationRequested;
            }
        }

        public uint LastCollectedPost { get; private set; } = uint.MaxValue;

        public uint FirstCollectedPost { get; private set; } = uint.MaxValue;

        public List<TorBridgesDiscoveryCS.Bridge.Bridge> CollectedBridges { get; private set; } = new List<TorBridgesDiscoveryCS.Bridge.Bridge>();

        public event EventHandler<OnNewCollectedBridgeEventArgs>? OnNewCollectedBridge;

        public event EventHandler<OnCollectionEndedEventArgs>? OnCollectionEnded;

        private CancellationTokenSource _cancellationTokenSource;

        
        /// <summary>
        /// Creates new TelegramChannelPostsCollector
        /// </summary>
        /// <param name="tgChannelHandle">Telegram channel handle (without @), e.g tor_bridges</param>
        public TelegramChannelBridesCollector(string tgChannelHandle)
        {
            TelegramChannelHandle = tgChannelHandle;
            _cancellationTokenSource = new();
            _cancellationTokenSource.Cancel();
        }

        public void Start()
        {
            if (Scanning) return;
            _cancellationTokenSource = new();
            Task.Factory.StartNew(CollectionWork, _cancellationTokenSource.Token);
        }

        public void Stop()
        {
            if (!Scanning) return;
            _cancellationTokenSource.Cancel();
        }

        public void WaitForEnd()
        {
            if (!Scanning) return;
            _cancellationTokenSource.Token.WaitHandle.WaitOne();
        }

        public void WaitForEnd(TimeSpan timeout)
        {
            if (!Scanning) return;
            _cancellationTokenSource.Token.WaitHandle.WaitOne(timeout);
        }

        public void CollectionWork()
        {
            try
            {
                while (LastCollectedPost > 1 && !_cancellationTokenSource.IsCancellationRequested)
                {
                    string lastPageContent = string.Empty;
                    try
                    {
                        lastPageContent = NetworkUtils.GetAsString("https://t.me/s/" + TelegramChannelHandle + "?before=" + LastCollectedPost);
                    }
                    catch (Exception)
                    {
                        continue;
                    }

                    MatchCollection obfs4BridgesMatches = BridgeUtils.Obfs4SelectorRegex.Matches(lastPageContent);
                    MatchCollection webtunnelBridgesMatches = BridgeUtils.WebtunnelSelectorRegex.Matches(lastPageContent);
                    foreach (Match obfs4match in obfs4BridgesMatches)
                    {
                        Obfs4Bridge? parsedObfs4;
                        if (Obfs4Bridge.TryParse(obfs4match.Value, out parsedObfs4))
                        {
                            if (!CollectedBridges.Any(x => x.Equals(parsedObfs4)))
                            {
#pragma warning disable CS8604 // Possible null reference argument; if TryParse returns true, parsedObfs4 is not null
                                CollectedBridges.Add(parsedObfs4);
#pragma warning restore CS8604 // Possible null reference argument.
                                OnNewCollectedBridge?.Invoke(this, new OnNewCollectedBridgeEventArgs(parsedObfs4));
                            }
                        }
                    }
                    foreach (Match webtunnelmatch in webtunnelBridgesMatches)
                    {
                        WebtunnelBridge? parsedWebtunnel;
                        if (WebtunnelBridge.TryParse(webtunnelmatch.Value, out parsedWebtunnel))
                        {
                            if (!CollectedBridges.Any(x => x.Equals(parsedWebtunnel)))
                            {
#pragma warning disable CS8604 // Possible null reference argument; if TryParse returns true, parsedWebtunnel is not null
                                CollectedBridges.Add(parsedWebtunnel);
#pragma warning restore CS8604 // Possible null reference argument.
                                OnNewCollectedBridge?.Invoke(this, new OnNewCollectedBridgeEventArgs(parsedWebtunnel));
                            }
                        }
                    }

                    List<string> lines = lastPageContent.Split("\n").ToList();
                    string? hrefLine = lines.Find(x => x.Contains("<div class=\"tgme_widget_message_centered js-messages_more_wrap\">"));
                    if (hrefLine == null)
                    {
                        // maybe?
                        LastCollectedPost = 0;
                        FirstCollectedPost = 0;
                        break;
                    }
                    string lastPostIndexString;
                    try
                    {
                        lastPostIndexString = hrefLine.Split("<a href=\"/s/" + TelegramChannelHandle + "?before=", 2)[1].Split('"', 2)[0];
                    }
                    catch (Exception)
                    {
                        LastCollectedPost = 0;
                        break;
                    }
                    if (LastCollectedPost == uint.MaxValue)
                    {
                        FirstCollectedPost = uint.Parse(lastPostIndexString);
                    }
                    LastCollectedPost = uint.Parse(lastPostIndexString);
                }
            }
            finally
            {
                if (!_cancellationTokenSource.IsCancellationRequested) _cancellationTokenSource.Cancel();
                _cancellationTokenSource.Dispose();
                OnCollectionEnded?.Invoke(this, new OnCollectionEndedEventArgs(this));
            }
        }

        public static void WaitAll(IEnumerable<TelegramChannelBridesCollector> collectors)
        {
            foreach (TelegramChannelBridesCollector collector in collectors)
            {
                collector.WaitForEnd();
            }
        }

        public static TorBridgesDiscoveryCS.Bridge.Bridge[] SumCollectedBridges(IEnumerable<TelegramChannelBridesCollector> collectors)
        {
            List<TorBridgesDiscoveryCS.Bridge.Bridge> collected = new List<TorBridgesDiscoveryCS.Bridge.Bridge>();
            foreach (TelegramChannelBridesCollector collector in collectors)
            {
                collected.AddRange(collector.CollectedBridges);
            }
            return [.. collected];
        }
    }

    internal sealed class OnNewCollectedBridgeEventArgs : EventArgs
    {

        public readonly TorBridgesDiscoveryCS.Bridge.Bridge Bridge;

        public OnNewCollectedBridgeEventArgs(TorBridgesDiscoveryCS.Bridge.Bridge bridge) : base()
        {
            Bridge = bridge;
        }
    }

    internal sealed class OnCollectionEndedEventArgs : EventArgs
    {

        public readonly TelegramChannelBridesCollector BridesCollector;

        public OnCollectionEndedEventArgs(TelegramChannelBridesCollector bridesCollector) : base()
        {
            BridesCollector = bridesCollector;
        }
    }
}
