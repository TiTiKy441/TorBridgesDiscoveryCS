# TorBridgesDiscoveryCS
Powerful tor bridges aggregation and scanning utility capable of automatic reachability scanning and pulling bridges from files, urls and public telegram channels.
This utility could be used to collect and scan for reachability large amounts of tor obfs4 and webtunnel bridges. Tor bridges could be collected from any publicly available telegram channels, local files and URLs and then be scanned for reachability. 

## Supported platforms 
Currently tested on Windows 10 x64

Basic functionality should be available on linux.

## How to use
```
Description:
  Powerful tor bridges aggregation and scanning utility capable of automatic reachability scanning and pulling bridges from files, urls and public telegram channels

Usage:
  TorBridgesDiscoveryCS [options]

Options:
  -?, -h, --help      Show help and usage information
  --version           Show version information
  -n                  The number of concurrent relays tested [default: 50]
  -g                  Test until at least this number of working relays are found [default: 3]
  --timeout           Socket connection timeout in milliseconds [default: 750]
  --channels          Telegram channels handles (without @) separated by a semicolon [default: RuTorBridgesObfs4Webtunnel]
  --urls              Collect additional bridges from these URLs, separated by a semicolon []
  --files             Collect additional bridges from these files, separated by a semicolon []
  --proxy             Set proxy for telegram posts download. Format: http://user:pass@host:port; socks5h://user:pass@host:port []
  --output-collected  File to output all relays collected from telegram channels (not checked for reachability) []
  --output-reachable  File to output reachable relays []
  --torrc             Write bridges to files in torrc format [default: False]
  --unique-only       Select only unique bridges for scanning [default: True]
  --scan              Scan bridges for reachability [default: True]
```

## How it works:
Program downloads all posts from the supplied telegram channels, then it selects `obfs4` and `webtunnel` bridges from the pulled text using Regex.

Then program does the same for URLs and files.

If proxy is set, channels and URLs are contacted via proxies.

After that if `--unique-only` is set, all the repeating bridges would be removed from collected bridges (matching addresses, matching fingerprints)

After that if `--output-collected` is set, all the bridges would be putted into a file (in torrc format if `--torrc` flag is set)

After that if `--scan` is set, bridges would be scanned for reachability via TCP socket connection checks.

`-n` determines how much bridges are contacted at the same time

`--timeout` determines the timeout after which the bridge is considered unreachable

As the scan goes, reachable relays would be printed in the console

After the scan ends, if the `--output-reachable` is set, all reachable relays would be outputted into a file (in torrc format if `--torrc` flag is set)

## Powerful usage example:
```
TorBridgesDiscoveryCS --channels tor_bridges;RuTorBridgesObfs4Webtunnel --output-collected collected.txt --output-reachable reachable.txt -g 9999 --timeout 750 --urls https://github.com/scriptzteam/Tor-Bridges-Collector/blob/main/bridges-obfs4?raw=true;https://github.com/scriptzteam/Tor-Bridges-Collector/blob/main/bridges-webtunnel?raw=true;https://github.com/scriptzteam/Tor-Bridges-Collector/blob/main/bridges-obfs4-ipv6?raw=true
```
