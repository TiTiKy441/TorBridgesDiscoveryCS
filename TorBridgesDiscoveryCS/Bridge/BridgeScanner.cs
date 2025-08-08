using System.Net.Sockets;
using System.Net;

namespace TorBridgesDiscoveryCS.Bridge
{
    internal sealed class BridgeScanner
    {

        public static bool Scanning
        {
            get
            {
                return _cancellationTokenSource != null && !_cancellationTokenSource.IsCancellationRequested;
            }
        }

        private static CancellationTokenSource _cancellationTokenSource;

        private static List<Bridge> _allBridges = new();
        private static List<Bridge> _workingBridges = new();

        public static event EventHandler? OnScanEnded;
        public static event EventHandler<OnNewWorkingRelayEventArgs>? OnNewWorkingRelay;

        public static Task? StartScan(TimeSpan timeout, int packetSize, Bridge[] bridgeToScan, int[]? port = null)
        {
            if (Scanning) return null;//throw new InvalidOperationException("Scan is already in progress");

            _allBridges = bridgeToScan.ToList();

            //Utils.Random.Shuffle(_allBridges);

            _cancellationTokenSource = new();
            return Task.Factory.StartNew(() => ScanWork(timeout, packetSize, port), _cancellationTokenSource.Token);
        }

        public static void StopScan()
        {
            if (!Scanning) return;//throw new InvalidOperationException("Scan is not in progress");
            _cancellationTokenSource.Cancel();
        }

        public static void WaitForEnd()
        {
            if (!Scanning) return;
            _cancellationTokenSource.Token.WaitHandle.WaitOne();
        }

        private static void ScanWork(TimeSpan timeout, int packetSize, int[]? ports = null)
        {
            try
            {
                int pointer = 0;
                Bridge test;

                while (pointer < _allBridges.Count && !_cancellationTokenSource.IsCancellationRequested)
                {
                    int created = 0;
                    int completed = 0;
                    while (pointer < _allBridges.Count && created < packetSize && !_cancellationTokenSource.IsCancellationRequested)
                    {
                        test = _allBridges[pointer++];
                        created++;
                        Test(test, timeout).ContinueWith(t => { Interlocked.Increment(ref completed); });
                    }
                    while (completed < created)
                    {
                        _cancellationTokenSource.Token.WaitHandle.WaitOne(1);
                    }
                }
            }
            finally
            {
                if (!_cancellationTokenSource.IsCancellationRequested) _cancellationTokenSource.Cancel();
                _cancellationTokenSource.Dispose();
                OnScanEnded?.Invoke(null, EventArgs.Empty);
            }
        }

        private static Task Test(Bridge bridge, TimeSpan timeout)
        {
            IPEndPoint addr = bridge.GetIpEndPoint();
            
            if (bridge is WebtunnelBridge webtunnelBridge)
            {
                try
                {
                    addr = new IPEndPoint(Dns.GetHostAddresses(webtunnelBridge.URL.Host)[0], 443);
                }
                catch (Exception)
                {
                    return Task.CompletedTask;
                }
            }
            Socket client = new(SocketType.Stream, ProtocolType.Tcp);
            return client.ConnectAsync(addr, _cancellationTokenSource.Token).AsTask().WaitAsync(timeout, _cancellationTokenSource.Token).ContinueWith(t =>
            {
                if (t.IsCompletedSuccessfully && !_cancellationTokenSource.IsCancellationRequested)
                {
                    lock (_workingBridges)
                    {
                        if (!_workingBridges.Any(x => x.Equals(bridge)))
                        {
                            OnNewWorkingRelay?.Invoke(null, new OnNewWorkingRelayEventArgs(bridge));
                            _workingBridges.Add(bridge);
                        }
                    }
                }
                client.Close();
                client.Dispose();
            }, _cancellationTokenSource.Token);
        }
    }

    internal sealed class OnNewWorkingRelayEventArgs : EventArgs
    {

        public readonly Bridge Bridge;

        public OnNewWorkingRelayEventArgs(Bridge bridge) : base()
        {
            Bridge = bridge;
        }
    }
}
