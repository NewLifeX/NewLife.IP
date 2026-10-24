# NewLife.IP - IP Address Database

![GitHub top language](https://img.shields.io/github/languages/top/newlifex/NewLife.IP?logo=github)


![GitHub License](https://img.shields.io/github/license/NewLifeX/NewLife.IP?logo=github)
![NuGet Downloads](https://img.shields.io/nuget/dt/NewLife.IP?logo=nuget)
![NuGet](https://img.shields.io/nuget/v/NewLife.IP?logo=nuget)
![NuGet prerelease](https://img.shields.io/nuget/vpre/NewLife.IP?label=dev%20nuget&logo=nuget)
[![test](https://github.com/NewLifeX/NewLife.IP/actions/workflows/test.yml/badge.svg?branch=master)](https://github.com/NewLifeX/NewLife.IP/actions/workflows/test.yml)
[![locales](https://img.shields.io/badge/locales-32-2ea44f?logo=dotnet)](Doc/多语言本地化.md)
![platform](https://img.shields.io/badge/platform-Windows%20%7C%20Linux%20%7C%20macOS%20%7C%20Android%20%7C%20iOS-blue)
![arch](https://img.shields.io/badge/arch-x64%20%7C%20arm64%20%7C%20loongarch64-orange)
[![contributors](https://img.shields.io/github/contributors/NewLifeX/NewLife.IP?logo=github)](https://github.com/NewLifeX/NewLife.IP/graphs/contributors)

<!-- lang:start -->
🌐 [简体中文](README.zh-CN.md) | [繁體中文](README.zh-TW.md) | [Español](README.es.md) | [Deutsch](README.de.md) | [日本語](README.ja.md) | [Français](README.fr.md) | [Português](README.pt.md) | [Русский](README.ru.md) | [한국어](README.ko.md) | [العربية](README.ar.md)
<!-- lang:end -->

A high-performance, cross-platform IP address geolocation library (IPv4) that **supports both local offline and online API data sources, selected by the user as needed**. Offline mode performs binary search directly against the decompressed raw database file through memory-mapped files (MemoryMappedFile, MMF), avoiding the managed memory pressure of loading the whole table, making it suitable for server and high-concurrency scenarios. Online mode calls a third-party web API (ip-api.com by default, with caching and fault tolerance) to obtain location data, suitable for scenarios with high data-freshness requirements.

Source: https://github.com/NewLifeX/NewLife.IP  
NuGet: `NewLife.IP`

---
## Documentation (Docs)
- [Requirements](Doc/需求文档.md) — vision, core goals and feature direction
- [Feature List](Doc/功能清单.md) — implementation / test / comment three-dimensional status tracking
- [Architecture Design](Doc/架构设计.md) — layers, components and key flows
- [Competitive Analysis](Doc/竞品分析.md) — static vs. online library comparison and gap analysis
- [Localization](Doc/多语言本地化.md) — runtime word tables, prefixed logging and verification scripts

---
## Features
- Multi-target frameworks: net45 ~ net10.0 plus netstandard2.0 / 2.1, one package for all
- Zero third-party runtime dependencies (relies only on the `NewLife.Core` basics)
- AOT friendly: NativeAOT smoke tests (win-x64 / linux-x64 / linux-arm64) plus LoongArch LA64 verification, with CI unit tests covering four RIDs
- Automatic download & update of IP data files (configurable data directory and download source)
- GZip-compressed database file (`ip.gz`), automatically decompressed to a temp file on first use and then opened via MMF
- Binary search with O(log N) complexity; compact index structure (7 bytes / record + variable-length strings)
- Thread-safe concurrent queries (read-only + ThreadStatic buffers)
- Convenient extension methods: `string.IPToAddress()` / `IPAddress.ToUInt32()` and more
- Pluggable into the `NewLife.Net` ecosystem, with unified resolution via `IpResolver.Register()`
- **Dual data sources**: local offline (MMF, millisecond response) + online API (optional, ip-api.com by default, with caching and fault tolerance), toggled with one switch via `IpResolver.Online`
- Runtime localization: word tables for 31 languages (aligned with NewLife.Core, English fallback); logging supports injected loggers, instance prefixes and an independent log language
- MIT license, easy for commercial use / secondary development

---
## Installation (Install)
NuGet:
```
dotnet add package NewLife.IP
```
Or reference it in `csproj`:
```xml
<PackageReference Include="NewLife.IP" Version="*" />
```

---
## Quick Start
```csharp
using NewLife.IP;
using System;

// Option 1: use extension methods directly (registers the resolver and loads the database internally)
var full = "39.144.10.35".IPToAddress();
Console.WriteLine(full);          // China–Guangdong–Shenzhen  <ISP info>

// Option 2: manage the lifecycle manually
var ip = new Ip
{
    // Optional: custom database file (supports .gz). If empty, uses Setting.DataPath + ip.gz with auto-download
    DbFile = "data/ip.gz"
};
ip.Init();
var (area, addr) = ip.GetAddress("116.234.91.199");
Console.WriteLine(area);          // China–Shanghai–Shanghai
Console.WriteLine(addr);          // ISP / data center description

// Option 3: integrate with NewLife.Net
IpResolver.Register(); // registers the resolver with NetHelper.IpResolver
var geo = "61.160.219.25".IPToAddress();

// Option 4: online mode (optional, ip-api.com free API by default, with caching and fault tolerance)
IpResolver.Online = true; // after switching, IPToAddress uses the online API
var geo2 = "8.8.8.8".IPToAddress();
// Online lookup failures return empty and write a log without blocking the business; switch back with Online = false

// Option 5: online mode against a domestic provider (Tencent Location Service, requires your own free Key; high-precision province/city/district in mainland China)
var online = new OnlineIp
{
    Server = "https://apis.map.qq.com/ws/location/v1/ip?ip={0}&key={1}", // {0}=IP, {1}=Key
    Key = "your Tencent Location Service Key",
    Cache = MemoryCache.Instance, // optional: enable online lookup caching (NewLife.Caching)
};
var (area, addr) = online.GetAddress("116.234.91.199"); // area=China–Shanghai–Shanghai–Yangpu District, addr=China Telecom
// AMap / Baidu work the same way: just replace Server and Key; Parse detects the response format automatically
```

---
## Core Types
| Type | Purpose | Notes |
| ---- | ---- | ---- |
| `Ip` | High-level wrapper: database file location, download/update, public queries | Maintains an `IpDatabase` instance, lazily initialized via `Init()`
| `IpDatabase` | Low-level binary database access (MMF + binary search) | Reads the index (`IndexInfo`) + address strings
| `IndexInfo` | Index structure: Start/End/Offset | 7-byte index area, Offset points to the data block
| `IpHelper` | Extension methods converting IP to/from UInt32 / string | IPv4 Big-Endian representation
| `IpResolver` | Implements `NewLife.Net.IIPResolver` | Injectable into ecosystem components for unified resolution
| `OnlineIp` | Online API resolver (ip-api.com by default) | With caching and fault tolerance, toggled by `IpResolver.Online`
| `IpCsv` | DB-IP CSV → Pure IP (CZ88) format converter | Extended data source (CORE-5)

---
## Logging & Localization
```csharp
using NewLife;
using NewLife.Log;
using System.Globalization;

var ip = new Ip
{
    Log = myLogger,          // any ILog; defaults to XTrace.Log, Logger.Null disables it
    LogPrefix = "[IP] ",     // prefix appended after message translation
};
Localization.Culture = new CultureInfo("zh-CN");     // exceptions and UI text
Localization.LogCulture = new CultureInfo("zh-CN");  // log language (follows Culture when empty)
```
- Built-in word tables for 31 languages, plus legacy region aliases `iw`/`in`/`no`/`mo` (English fallback); generated at compile time and AOT-safe; missing translations automatically fall back to English.
- Logging goes through `InfoPrefixed` / `ErrorPrefixed`; prefixes do not participate in lookups, so dynamic instance names never break translations.
- See [Localization](Doc/多语言本地化.md) for details.

---
## Data Files & Update Mechanism
- Default file name: `ip.gz`, placed in `Setting.Current.DataPath` (from the `NewLife.Core` configuration system)
- If the local file is missing, too small (<3MB) or older than the built-in threshold date, a download is attempted from `Setting.PluginServer`
- On success: overwrite the old file and `Ip.Init()` rebuilds the MMF
- Users may supply their own data files in the same format (compatible with the Pure IP IPv4 library structure: index area + record area + GB18030 text, compatible with GB2312/GBK)
- When reading:
  1. If the file is `.gz`, decompress it to a temp file
  2. Open read-only via `MemoryMappedFile.CreateFromFile`
  3. Binary search: locate the record by start/end IP → parse area and address strings

---
## Query Flow (Brief)
1. IPv4 input => convert to `UInt32` (Big-Endian)
2. Binary search the index area: the `(start,end)` range that covers the IP is a hit
3. Read the pointed data segment: use the marker byte (1 / 2) to determine redirection
4. Decode the GB18030 byte string (compatible with GB2312/GBK), filter the reserved word `CZ88.NET`, and return `(area, addr)`

---
## Performance & Resources
- Index binary search: log2(N) levels; only ~18 comparisons for N≈hundreds of thousands of records
- MMF avoids loading everything into the managed heap at once; only the hit area produces minimal string allocations
- ThreadStatic buffer (starts at 64B, grows to 4KB as needed) reduces repeated allocations; GB18030 decoding targets only the hit segment, so long multilingual place names are never truncated
- On .NET Core/Android/iOS, code page providers are registered automatically on first use, so `IpDatabase` / `IpCsv` work even when used directly without manual registration
- Suitable for long-lived background services / gateways / API servers; CPU usage stays steady under high concurrency

(Concrete benchmark results will be added to the Release Notes as versions progress; PRs with measured data are welcome.)

---
## Thread Safety
- `Ip.Init()` uses double-checked locking to ensure the database is loaded only once
- Queries are read-only; `IpDatabase` never modifies the index structure internally
- `ReadString` uses a `[ThreadStatic]` buffer `_buf`, so multiple threads do not contend
- The same `Ip` instance or extension methods can be reused directly across threads / parallel tasks

---
## Integration with the NewLife Ecosystem
- `NewLife.Net`: after `IpResolver.Register()`, the framework uses the resolver internally for unified resolution
- Configuration: deployability via `NewLife.Core`'s `Setting` (`DataPath`, `PluginServer`)
- Logging: key events go through `ILog` (defaults to `XTrace.Log`), supporting custom loggers and instance prefixes; messages are localized according to `Localization.LogCulture`, and can feed into Stardust distributed tracing and log aggregation

---
## Custom Usage Scenarios
1. Online API service: call `new Ip().Init()` once during application startup and expose an HTTP endpoint
2. Real-time log annotation: use `string.IPToAddress()` in the logging pipeline to enrich access logs / security audits with geographic information
3. Security and risk control: use the location / ISP of login, transaction and alert source IPs for whitelisting or location-anomaly alerts
4. Batch offline processing: loop over an IP list and resolve in batches; reusing a singleton `Ip` is recommended

---
## Extension Methods at a Glance
| Method | Description |
| ---- | ---- |
| `string.IPToAddress()` | Returns the combined "area ISP" string |
| `string.ToUInt32IP()` | IPv4 string → UInt32 (Big-Endian) |
| `UInt32.ToStringIP()` | UInt32 → zero-padded IPv4 string |
| `IPAddress.ToUInt32()` | `IPAddress` → UInt32 |
| `UInt32.ToAddress()` | UInt32 → `IPAddress` |

---
## FAQ
Q: Is IPv6 supported?  
A: The current version focuses on IPv4. If you have a clear scenario and a public high-quality IPv6 data source, feel free to submit an Issue.  
Q: Where does the data come from?  
A: It uses a format compatible with the Pure IP (CZ88) database (including the redirection structure), and users can replace it with their own data.  
Q: Can I load everything into memory for acceleration?  
A: MMF is already lightweight enough and usually needs no extra caching; if you need a custom cache, wrap a result dictionary at a higher layer.  
Q: How do I disable automatic downloads?  
A: Pre-place an `ip.gz` with a valid size and date in the data directory, or use a read-only environment and manage updates yourself.  

---
## Running Unit Tests
The repository includes an xUnit v3 test project `XUnitTest` (Microsoft.Testing.Platform, with in-process execution supported directly by VS2026):
```
dotnet test --project XUnitTest/XUnitTest.csproj
```
Main verifications:
- Correctness of IP/UInt32 conversions
- Area consistency for common real-world IP lookups
- Stability of multi-threaded concurrent resolution
- Boundary behavior of the constructed library (out-of-range, index gaps, redirected records)

CI (triggered on push/PR) runs the full unit test suite on four RIDs: win-x64 / linux-x64 / linux-arm64 / linux-la64 (LoongArch, QEMU); an `AotSmoke` project performs NativeAOT smoke comparisons (see `.github/workflows/`).

---
## Versioning / Updates
- Semantic versioning: `MAJOR.MINOR.PATCH`; the current major version is 2.x
- Multiple TFMs included, with API kept backward compatible; breaking changes are marked `[Obsolete]` first
- Release Notes (NuGet / GitHub) highlight: performance / compatibility / security fixes

### Release
Push a `v`-prefixed tag (for example `v2.6.2026.1024`) to trigger `.github/workflows/publish.yml`. The workflow packs
the package, publishes it to both **NuGet.org** and **GitHub Packages**, and creates a **GitHub Release** whose notes are
taken from the matching `ChangeLog.md` section (assets: nupkg + snupkg). The package version is taken from the tag, and a
tag that does not match `VersionPrefix` fails the build. A manual run only publishes packages and creates no Release.

---
## Contributing
Welcome:
1. File Issues (bugs / new requirements / performance data / IPv6 proposals)
2. PRs: follow the code style (see `.github/copilot-instructions.md`)
3. Add benchmarks, data-source validation scripts, or internationalization support

Suggested PR checklist:
- [ ] Unit tests pass & new logic is covered
- [ ] No unnecessary third-party dependencies introduced
- [ ] Public API changes include migration notes
- [ ] Performance-sensitive changes include quantified data

---
## License
MIT. Free for commercial use / redistribution as long as the copyright notice is retained.

---
## Roadmap (Possible Directions)
- IPv6 support (requires a suitable fine-grained geolocation database & size assessment)
- More precise ISP / backbone network identification
- Optional incremental update mechanism (avoid full-package downloads)
- Benchmark & Prometheus statistics examples

---

## Ecosystem Map

NewLife is a layered ecosystem of independently versioned open-source projects. The map below is shared verbatim by every NewLife repository; each project links to its repository and its primary NuGet package.

| Project | What it does | Downloads |
| --- | --- | --- |
| **Foundation** | | |
| [NewLife.Core](https://github.com/NewLifeX/X) | Logging, configuration, caching, networking, serialization, scheduling and APM tracing. | [![NuGet](https://img.shields.io/nuget/dt/NewLife.Core?style=flat-square&logo=nuget&label=downloads)](https://www.nuget.org/packages/NewLife.Core) |
| [NewLife.XCode](https://github.com/NewLifeX/NewLife.XCode) | Data middleware: single tables at the tens-of-billions scale, automatic sharding, read-write splitting, 10+ databases. | [![NuGet](https://img.shields.io/nuget/dt/NewLife.XCode?style=flat-square&logo=nuget&label=downloads)](https://www.nuget.org/packages/NewLife.XCode) |
| [NewLife.Net](https://github.com/NewLifeX/NewLife.Net) | High-performance network library: 1.77M pps / 14.5 Gbps on a single machine (TCP 1KB, 2026-10), 4M long-lived TCP connections. | [![NuGet](https://img.shields.io/nuget/dt/NewLife.Net?style=flat-square&logo=nuget&label=downloads)](https://www.nuget.org/packages/NewLife.Net) |
| [NewLife.Remoting](https://github.com/NewLifeX/NewLife.Remoting) | Protocol communication and RPC framework for client/server applications and IoT devices. | [![NuGet](https://img.shields.io/nuget/dt/NewLife.Remoting?style=flat-square&logo=nuget&label=downloads)](https://www.nuget.org/packages/NewLife.Remoting) |
| [NewLife.Agent](https://github.com/NewLifeX/NewLife.Agent) | Installs .NET applications as OS daemons: Windows services and Linux systemd. | [![NuGet](https://img.shields.io/nuget/dt/NewLife.Agent?style=flat-square&logo=nuget&label=downloads)](https://www.nuget.org/packages/NewLife.Agent) |
| [NewLife.Templates](https://github.com/NewLifeX/NewLife.Zero) | `dotnet new` project templates for console, web, API, service, data and IoT projects. | [![NuGet](https://img.shields.io/nuget/dt/NewLife.Templates?style=flat-square&logo=nuget&label=downloads)](https://www.nuget.org/packages/NewLife.Templates) |
| **Data & Messaging** | | |
| [NewLife.Redis](https://github.com/NewLifeX/NewLife.Redis) | Redis client with microsecond latency, millions of ops/s and built-in message queues. | [![NuGet](https://img.shields.io/nuget/dt/NewLife.Redis?style=flat-square&logo=nuget&label=downloads)](https://www.nuget.org/packages/NewLife.Redis) |
| [NewLife.RocketMQ](https://github.com/NewLifeX/NewLife.RocketMQ) | Pure managed RocketMQ client for Apache RocketMQ and Alibaba Cloud Message Queue. | [![NuGet](https://img.shields.io/nuget/dt/NewLife.RocketMQ?style=flat-square&logo=nuget&label=downloads)](https://www.nuget.org/packages/NewLife.RocketMQ) |
| [NewLife.MQTT](https://github.com/NewLifeX/NewLife.MQTT) | MQTT client and server, including Alibaba Cloud IoT connectivity. | [![NuGet](https://img.shields.io/nuget/dt/NewLife.MQTT?style=flat-square&logo=nuget&label=downloads)](https://www.nuget.org/packages/NewLife.MQTT) |
| [NewLife.NovaDb](https://github.com/NewLifeX/NewLife.NovaDb) | Hybrid database in C#: embedded or networked, relational, time-series, message queue and KV in one. | [![NuGet](https://img.shields.io/nuget/dt/NewLife.NovaDb?style=flat-square&logo=nuget&label=downloads)](https://www.nuget.org/packages/NewLife.NovaDb) |
| [NewLife.MySql](https://github.com/NewLifeX/NewLife.MySql) | MySql driver and helper layer built on the NewLife data stack. | [![NuGet](https://img.shields.io/nuget/dt/NewLife.MySql?style=flat-square&logo=nuget&label=downloads)](https://www.nuget.org/packages/NewLife.MySql) |
| **Web, AI & Distributed** | | |
| [NewLife.Cube](https://github.com/NewLifeX/NewLife.Cube) | Rapid admin-platform framework: permissions, SSO, OAuth server and CRUD scaffolding. | [![NuGet](https://img.shields.io/nuget/dt/NewLife.Cube?style=flat-square&logo=nuget&label=downloads)](https://www.nuget.org/packages/NewLife.Cube) |
| [Stardust](https://github.com/NewLifeX/Stardust) | Distributed service platform: node management, APM, configuration center, registry and release center. | [![NuGet](https://img.shields.io/nuget/dt/NewLife.Stardust?style=flat-square&logo=nuget&label=downloads)](https://www.nuget.org/packages/NewLife.Stardust) |
| [AntJob](https://github.com/NewLifeX/AntJob) | Distributed task and real-time data scheduling platform with sharding, retries and backfill. | [![NuGet](https://img.shields.io/nuget/dt/NewLife.AntJob?style=flat-square&logo=nuget&label=downloads)](https://www.nuget.org/packages/NewLife.AntJob) |
| [NewLife.AI](https://github.com/NewLifeX/NewLife.AI) | Unified client for 46 LLM providers plus ChatAI, a ready-to-run web chat application. | [![NuGet](https://img.shields.io/nuget/dt/NewLife.AI?style=flat-square&logo=nuget&label=downloads)](https://www.nuget.org/packages/NewLife.AI) |
| **Industrial & IoT** | | |
| [NewLife.IoT](https://github.com/NewLifeX/NewLife.IoT) | IoT standard library defining protocol contracts and device abstractions. | [![NuGet](https://img.shields.io/nuget/dt/NewLife.IoT?style=flat-square&logo=nuget&label=downloads)](https://www.nuget.org/packages/NewLife.IoT) |
| [NewLife.Modbus](https://github.com/NewLifeX/NewLife.Modbus) | Modbus TCP/RTU/ASCII implementation on top of NewLife.IoT. | [![NuGet](https://img.shields.io/nuget/dt/NewLife.Modbus?style=flat-square&logo=nuget&label=downloads)](https://www.nuget.org/packages/NewLife.Modbus) |
| [NewLife.Siemens](https://github.com/NewLifeX/NewLife.Siemens) | Siemens PLC protocol on top of NewLife.IoT. | [![NuGet](https://img.shields.io/nuget/dt/NewLife.Siemens?style=flat-square&logo=nuget&label=downloads)](https://www.nuget.org/packages/NewLife.Siemens) |
| [NewLife.Omron](https://github.com/NewLifeX/NewLife.Omron) | Omron PLC protocol on top of NewLife.IoT. | [![NuGet](https://img.shields.io/nuget/dt/NewLife.Omron?style=flat-square&logo=nuget&label=downloads)](https://www.nuget.org/packages/NewLife.Omron) |
| [NewLife.Melsec](https://github.com/NewLifeX/NewLife.Melsec) | Mitsubishi Melsec PLC protocol on top of NewLife.IoT. | [![NuGet](https://img.shields.io/nuget/dt/NewLife.Melsec?style=flat-square&logo=nuget&label=downloads)](https://www.nuget.org/packages/NewLife.Melsec) |
| [NewLife.OPC](https://github.com/NewLifeX/NewLife.OPC) | OPC client for industrial data access. | — |
| [NewLife.BACnet](https://github.com/NewLifeX/NewLife.BACnet) | BACnet building-automation protocol stack. | [![NuGet](https://img.shields.io/nuget/dt/NewLife.BACnet?style=flat-square&logo=nuget&label=downloads)](https://www.nuget.org/packages/NewLife.BACnet) |
| [ZeroIoT](https://github.com/NewLifeX/ZeroIoT) | Lightweight open-source IoT platform with gateway and device management. | — |
| **Vehicle Telematics** | | |
| [NewLife.JT808](https://github.com/NewLifeX/NewLife.JT808) | JT/T 808 vehicle positioning protocol: 70+ message bodies, 2011/2013/2019. | [![NuGet](https://img.shields.io/nuget/dt/NewLife.JT808?style=flat-square&logo=nuget&label=downloads)](https://www.nuget.org/packages/NewLife.JT808) |
| [NewLife.JT809](https://github.com/NewLifeX/NewLife.JT809) | JT/T 809 platform-to-platform protocol for transport authorities. | [![NuGet](https://img.shields.io/nuget/dt/NewLife.JT809?style=flat-square&logo=nuget&label=downloads)](https://www.nuget.org/packages/NewLife.JT809) |
| [NewLife.JT1078](https://github.com/NewLifeX/NewLife.JT1078) | JT/T 1078 in-vehicle video: RTP parsing, FLV/fMP4 remuxing and live streaming. | [![NuGet](https://img.shields.io/nuget/dt/NewLife.JT1078?style=flat-square&logo=nuget&label=downloads)](https://www.nuget.org/packages/NewLife.JT1078) |
| **Domain Components** | | |
| [NewLife.Office](https://github.com/NewLifeX/NewLife.Office) | One package for Excel, Word, PPT, PDF and 15+ document formats. | [![NuGet](https://img.shields.io/nuget/dt/NewLife.Office?style=flat-square&logo=nuget&label=downloads)](https://www.nuget.org/packages/NewLife.Office) |
| [NewLife.Audio](https://github.com/NewLifeX/NewLife.Audio) | Cross-platform audio: codecs, device I/O, DSP, containers and streaming. | [![NuGet](https://img.shields.io/nuget/dt/NewLife.Audio?style=flat-square&logo=nuget&label=downloads)](https://www.nuget.org/packages/NewLife.Audio) |
| [NewLife.Video](https://github.com/NewLifeX/NewLife.Video) | Video codecs, container formats, frame processing and streaming. | [![NuGet](https://img.shields.io/nuget/dt/NewLife.Video?style=flat-square&logo=nuget&label=downloads)](https://www.nuget.org/packages/NewLife.Video) |
| [NewLife.Map](https://github.com/NewLifeX/NewLife.Map) | Unified interface over Baidu, AMap, Tencent and Tianditu with geocoding and coordinate conversion. | [![NuGet](https://img.shields.io/nuget/dt/NewLife.Map?style=flat-square&logo=nuget&label=downloads)](https://www.nuget.org/packages/NewLife.Map) |
| [NewLife.IP](https://github.com/NewLifeX/NewLife.IP) | IPv4 geolocation with an offline memory-mapped database and an online API fallback. | [![NuGet](https://img.shields.io/nuget/dt/NewLife.IP?style=flat-square&logo=nuget&label=downloads)](https://www.nuget.org/packages/NewLife.IP) |
| **Tools & Products** | | |
| [CrazyCoder](https://github.com/NewLifeX/XCoder) | Developer toolbox: network, serial port, crypto, regex, Modbus and MQTT. | [![NuGet](https://img.shields.io/nuget/dt/NewLife.XCoder?style=flat-square&logo=nuget&label=downloads)](https://www.nuget.org/packages/NewLife.XCoder) |
| [GitCandy](https://github.com/NewLifeX/GitCandy) | Self-hosted Git source code management system. | — |
| [LuckyClover](https://github.com/NewLifeX/LuckyClover) | Installs the matching .NET runtime automatically when launched from an older host. | — |

> Badges show the primary package of each project; theme, extension and satellite packages are listed on their NuGet pages. The 30+ packages not listed here (FTP, LoRa, Office extensions, protocol drivers and more) are available under the [NewLifeX organization](https://github.com/NewLifeX) and on [newlifex.com](https://newlifex.com).

## Team

<img src="https://newlifex.com/logo.png" width="48" align="right" alt="NewLife" />

Founded in 2002, the NewLife team has spent more than two decades on the .NET and IoT ecosystem, maintains nearly 100 open-source projects (34 of them core to this map), and delivers integrated software and hardware solutions for power, logistics, healthcare, industrial control and internet industries. We live by the motto "learning has no seniority — mastery earns respect".

Open-source components have been downloaded <!--nuget-total:start-->6.48M<!--nuget-total:end--> times from NuGet (ecosystem-wide, as of 2026-10).

Website: https://newlifex.com · Open source organization: https://github.com/NewLifeX

<details>
<summary>WeChat Official Account: 智能大石头 (scan to follow)</summary>

![智能大石头](https://newlifex.com/stone.jpg)

</details>
