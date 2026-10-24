# NewLife.IP - IP地址库

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
🌐 [English](README.md) | [繁體中文](README.zh-TW.md) | [Español](README.es.md) | [Deutsch](README.de.md) | [日本語](README.ja.md) | [Français](README.fr.md) | [Português](README.pt.md) | [Русский](README.ru.md) | [한국어](README.ko.md) | [العربية](README.ar.md)
<!-- lang:end -->

> ℹ️ 本中文版与 [English README](README.md) 同批维护；若两者内容不一致，以英文版为对外口径。

高性能、跨平台的 IP 地址定位库（IPv4），**支持本地离线 + 在线 API 双数据源，由用户按需选择**。离线模式通过内存映射文件 (MemoryMappedFile, MMF) 直接对压缩解包后的原始数据库文件进行二分检索，避免整表加载造成的托管内存压力，适合服务端与高并发场景；在线模式调用第三方 Web API（默认 ip-api.com，含缓存与容错）获取归属地，适合数据新鲜度要求高的场景。

源码： https://github.com/NewLifeX/NewLife.IP  
Nuget：`NewLife.IP`

---
## 文档 (Docs)
- [需求文档](Doc/需求文档.md) — 愿景、核心目标与功能方向
- [功能清单](Doc/功能清单.md) — 实现 / 测试 / 注释三维状态追踪
- [架构设计](Doc/架构设计.md) — 分层、组件与关键流程
- [竞品分析](Doc/竞品分析.md) — 静态库 / 在线库竞品对比与差距分析
- [多语言本地化](Doc/多语言本地化.md) — 运行时词表、前缀日志与校验脚本

---
## 特性 (Features)
- 多目标框架：net45 ~ net10.0 及 netstandard2.0 / 2.1，一包通用
- 零第三方运行时依赖（仅依赖 `NewLife.Core` 基础能力）
- AOT 友好：NativeAOT 冒烟（win-x64 / linux-x64 / linux-arm64）+ 龙芯 LA64 验证，CI 单元测试覆盖四大 RID
- 自动下载 & 更新 IP 数据文件（可配置数据目录与下载源）
- GZip 压缩数据库文件（`ip.gz`），首次使用自动解压到临时文件再用 MMF 打开
- 二分查找，O(log N) 复杂度；索引结构紧凑（7 字节 / 记录 + 变长字符串）
- 线程安全的并发查询（只读 + ThreadStatic 缓冲）
- 提供便捷扩展方法：`string.IPToAddress()` / `IPAddress.ToUInt32()` 等
- 可插入到 `NewLife.Net` 生态，通过 `IpResolver.Register()` 统一解析
- **双数据源**：本地离线（MMF 秒回）+ 在线 API（可选，默认 ip-api.com，含缓存与容错），`IpResolver.Online` 一键切换
- 运行时多语言：31 种语言词表（对齐 NewLife.Core，英文兜底），日志支持注入日志对象、实例前缀与独立日志语言
- MIT 许可，便于商用 / 二次开发

---
## 安装 (Install)
NuGet:
```
dotnet add package NewLife.IP
```
或在 `csproj` 引用：
```xml
<PackageReference Include="NewLife.IP" Version="*" />
```

---
## 快速开始 (Quick Start)
```csharp
using NewLife.IP;
using System;

// 方式1：直接使用扩展方法（内部自动注册解析器并加载数据库）
var full = "39.144.10.35".IPToAddress();
Console.WriteLine(full);          // 中国–广东–深圳  <运营商信息>

// 方式2：手动控制生命周期
var ip = new Ip
{
    // 可选：自定义数据库文件（支持 .gz）。为空则使用 Setting.DataPath + ip.gz 自动下载
    DbFile = "data/ip.gz"
};
ip.Init();
var (area, addr) = ip.GetAddress("116.234.91.199");
Console.WriteLine(area);          // 中国–上海–上海
Console.WriteLine(addr);          // 运营商 / 机房描述

// 方式3：集成 NewLife.Net
IpResolver.Register(); // 将解析器注册到 NetHelper.IpResolver
var geo = "61.160.219.25".IPToAddress();

// 方式4：在线模式（可选，默认 ip-api.com 免费 API，含缓存与容错）
IpResolver.Online = true; // 切换后 IPToAddress 走在线 API
var geo2 = "8.8.8.8".IPToAddress();
// 在线查询失败返回空并写日志，不阻断业务；切回离线只需 Online = false

// 方式5：在线模式对接国内厂商（腾讯位置服务，需自行申请免费 Key，中国境内省市区县高精度）
var online = new OnlineIp
{
    Server = "https://apis.map.qq.com/ws/location/v1/ip?ip={0}&key={1}", // {0}=IP，{1}=Key
    Key = "你的腾讯位置服务Key",
    Cache = MemoryCache.Instance, // 可选：启用在线查询缓存（NewLife.Caching）
};
var (area, addr) = online.GetAddress("116.234.91.199"); // area=中国–上海市–上海市–杨浦区，addr=电信
// 高德 / 百度同构：仅需替换 Server 与 Key，Parse 自动识别响应格式
```

---
## 核心类型说明
| 类型 | 作用 | 备注 |
| ---- | ---- | ---- |
| `Ip` | 高层封装：数据库文件定位、下载更新、对外查询 | 维护 `IpDatabase` 实例，懒加载 `Init()`
| `IpDatabase` | 低层二进制数据库访问（MMF + 二分） | 读取索引（`IndexInfo`）+ 地址字符串
| `IndexInfo` | 索引结构：Start/End/Offset | 7 字节索引区，Offset 指向数据块
| `IpHelper` | IP 与 UInt32 / 字符串互转扩展方法 | IPv4 Big-Endian 表示
| `IpResolver` | 实现 `NewLife.Net.IIPResolver` | 可注入生态组件统一解析 |
| `OnlineIp` | 在线 API 解析器（默认 ip-api.com） | 含缓存与容错，`IpResolver.Online` 切换 |
| `IpCsv` | DB-IP CSV → 纯真格式转换器 | 扩展数据源（CORE-5） |

---
## 日志与本地化 (Logging & Localization)
```csharp
using NewLife;
using NewLife.Log;
using System.Globalization;

var ip = new Ip
{
    Log = myLogger,          // 任意 ILog；默认 XTrace.Log，Logger.Null 可关闭
    LogPrefix = "[IP] ",     // 前缀在消息翻译之后拼接
};
Localization.Culture = new CultureInfo("zh-CN");     // 异常与界面文案
Localization.LogCulture = new CultureInfo("zh-CN");  // 日志语言（为空时跟随 Culture）
```
- 内置 31 种语言词表，另含 `iw`/`in`/`no`/`mo` 旧式区域别名（英文兜底），编译期生成、AOT 安全；缺失译文自动回退英文。
- 日志统一走 `InfoPrefixed` / `ErrorPrefixed`，前缀不参与查表，动态实例名不会导致译文失效。
- 详见 [多语言本地化](Doc/多语言本地化.md)。

---
## 数据文件与更新机制
- 默认文件名：`ip.gz`，放置于 `Setting.Current.DataPath`（来自 `NewLife.Core` 配置体系）
- 若本地文件不存在、过小 (<3MB) 或早于内置阈值日期，则尝试从 `Setting.PluginServer` 下载
- 下载成功：覆盖旧文件，`Ip.Init()` 重新构建 MMF
- 支持用户自备同格式数据文件（兼容纯真 IPv4 库结构：索引区 + 记录区 + GB18030 文本，兼容 GB2312/GBK）
- 读取时：
  1. 若为 `.gz` 则解压到临时文件
  2. 使用 `MemoryMappedFile.CreateFromFile` 方式只读访问
  3. 二分查找：通过起止 IP 定位记录 → 解析区域与地址字符串

---
## 查询流程 (简述)
1. 输入 IPv4 => 转为 `UInt32` (Big-Endian)
2. 在索引区做二分：`(start,end)` 覆盖范围即命中
3. 读取指向的数据段：按标记字节 (1 / 2) 判断是否重定向
4. 解析 GB18030 字节串（兼容 GB2312/GBK），过滤保留字 `CZ88.NET`，返回 `(area, addr)`

---
## 性能与资源
- 索引二分：log2(N) 级；N≈数十万记录时仅 ~18 次比较
- MMF 避免一次性全部装入托管堆；仅在命中区域产生极少字符串分配
- ThreadStatic 缓冲区（起始 64B，按需扩容至 4KB）降低重复分配；GB18030 解码仅针对命中段，多语言长地名不会截断
- .NET Core/Android/iOS 首次使用时自动注册代码页提供程序，直接使用 `IpDatabase` / `IpCsv` 也无需手工注册
- 适合长生命周期后台服务 / 网关 / API Server；在高并发下 CPU 占用平稳

(具体 Benchmark 会随版本在 Release Notes 中补充，欢迎 PR 补测数据。)

---
## 线程安全 (Thread Safety)
- `Ip.Init()` 双重检查 + 锁，确保数据库单次加载
- 查询只读；`IpDatabase` 内部不修改索引结构
- `ReadString` 使用 `[ThreadStatic]` 缓冲 `_buf`，多线程无竞争
- 可在多线程 / 并行任务中直接复用同一个 `Ip` 实例或扩展方法

---
## 与 NewLife 生态的集成
- `NewLife.Net`：`IpResolver.Register()` 后，框架内部统一调用解析
- 配置：借助 `NewLife.Core` 的 `Setting`（`DataPath`, `PluginServer`）实现可部署化
- 日志：关键事件走 `ILog`（默认 `XTrace.Log`），支持自定义日志对象与实例前缀；消息按 `Localization.LogCulture` 本地化，可接入星尘 (Stardust) 分布式追踪与日志汇聚

---
## 自定义使用场景
1. 在线 API 服务：在应用启动阶段调用一次 `new Ip().Init()`，对外提供 HTTP 接口
2. 日志实时标注：在日志管线中使用 `string.IPToAddress()` 为访问日志 / 安全审计补充地理信息
3. 安全风控：对登录、交易、告警来源 IP 做归属地 / 运营商的白名单或异地提醒
4. 批量离线处理：循环读取 IP 列表，批量解析；建议复用单例 `Ip`

---
## 扩展方法速览
| 方法 | 说明 |
| ---- | ---- |
| `string.IPToAddress()` | 返回 "区域 运营商" 拼接字符串 |
| `string.ToUInt32IP()` | IPv4 字符串 → UInt32 (Big-Endian) |
| `UInt32.ToStringIP()` | UInt32 → 补零 IPv4 字符串 |
| `IPAddress.ToUInt32()` | `IPAddress` → UInt32 |
| `UInt32.ToAddress()` | UInt32 → `IPAddress` |

---
## FAQ
Q: 是否支持 IPv6?  
A: 当前版本专注 IPv4，若有明确场景 & 公开高质量 IPv6 数据源，欢迎提交 Issue。  
Q: 数据源从哪里来？  
A: 使用与纯真数据库兼容格式（含重定向结构），用户可自行替换。  
Q: 我能否只加载内存加速？  
A: MMF 已足够轻量，通常无需额外缓存；如需自定义缓存，可在上层封装结果字典。  
Q: 如何禁用自动下载？  
A: 预先放置合法体积 & 日期的 `ip.gz` 到数据目录，或设置只读环境并自行管理更新。  

---
## 运行单元测试
仓库包含 xUnit v3 测试工程 `XUnitTest`（Microsoft.Testing.Platform，VS2026 直接支持进程内执行）：
```
dotnet test --project XUnitTest/XUnitTest.csproj
```
主要验证：
- IP/UInt32 互转正确性
- 常见真实 IP 解析的区域一致性
- 多线程并发解析稳定性
- 构造库的边界行为（区间外、索引间隙、重定向记录）

CI（push/PR 触发）在 win-x64 / linux-x64 / linux-arm64 / linux-la64（龙芯，QEMU）四大 RID 运行全量单测；另有 `AotSmoke` 工程做 NativeAOT 冒烟对照（见 `.github/workflows/`）。

---
## 版本策略 / 更新
- 语义化版本：`MAJOR.MINOR.PATCH`，当前主版本为 2.x
- 包含多 TFM，保持 API 向后兼容；破坏性变更会先标记 `[Obsolete]`
- Release Notes（NuGet / GitHub）突出：性能 / 兼容 / 安全修复

### 发版
推送 `v` 前缀 tag（如 `v2.6.2026.1024`）触发 `.github/workflows/publish.yml`。工作流完成打包后同时发布到 NuGet.org 与 GitHub Packages，并创建 GitHub Release（说明取自 `ChangeLog.md` 对应段落，资产含 nupkg/snupkg）；包版本严格取自 tag，tag 与 `VersionPrefix` 不一致会直接失败；手工触发工作流只发包，不创建 Release。

---
## 贡献 (Contributing)
欢迎：
1. 提交 Issue（缺陷 / 新需求 / 性能数据 / IPv6 方案）
2. PR：保持代码风格（参见 `.github/copilot-instructions.md`）
3. 补充 Benchmark、数据源校验脚本、或国际化支持

PR Checklist 建议：
- [ ] 单元测试通过 & 覆盖新增逻辑
- [ ] 未引入不必要的第三方依赖
- [ ] 公共 API 变更已给出迁移说明
- [ ] 性能敏感更改附带量化说明

---
## License
MIT. 保留版权声明即可自由商用 / 二次分发。

---
## Roadmap (可能方向)
- IPv6 支持（需要合适的精细地理库 & 体积评估）
- 更精细的运营商 / 骨干网识别
- 可选增量更新机制（避免整包下载）
- Benchmark & Prometheus 统计示例

---

## 生态版图

NewLife 生态按依赖分层，各项目独立版本、独立发版。下面的版图在所有 NewLife 仓库中逐字一致；每个项目链接到代码仓库与主 NuGet 包。

| 项目 | 一句话定位 | 下载量 |
| --- | --- | --- |
| **基础底座** | | |
| [NewLife.Core](https://github.com/NewLifeX/X) | 日志、配置、缓存、网络、序列化、定时调度与 APM 追踪。 | [![NuGet](https://img.shields.io/nuget/dt/NewLife.Core?style=flat-square&logo=nuget&label=downloads)](https://www.nuget.org/packages/NewLife.Core) |
| [NewLife.XCode](https://github.com/NewLifeX/NewLife.XCode) | 数据中间件：单表百亿级、自动分表、读写分离，支持 10 多种数据库。 | [![NuGet](https://img.shields.io/nuget/dt/NewLife.XCode?style=flat-square&logo=nuget&label=downloads)](https://www.nuget.org/packages/NewLife.XCode) |
| [NewLife.Net](https://github.com/NewLifeX/NewLife.Net) | 高性能网络库：单机 177 万 pps、14.5 Gbps（TCP 1KB，2026-10 实测），400 万 Tcp 长连接。 | [![NuGet](https://img.shields.io/nuget/dt/NewLife.Net?style=flat-square&logo=nuget&label=downloads)](https://www.nuget.org/packages/NewLife.Net) |
| [NewLife.Remoting](https://github.com/NewLifeX/NewLife.Remoting) | 协议通信与 RPC 框架，面向 CS 应用与物联网设备。 | [![NuGet](https://img.shields.io/nuget/dt/NewLife.Remoting?style=flat-square&logo=nuget&label=downloads)](https://www.nuget.org/packages/NewLife.Remoting) |
| [NewLife.Agent](https://github.com/NewLifeX/NewLife.Agent) | 把 .NET 应用安装为操作系统守护进程：Windows 服务与 Linux systemd。 | [![NuGet](https://img.shields.io/nuget/dt/NewLife.Agent?style=flat-square&logo=nuget&label=downloads)](https://www.nuget.org/packages/NewLife.Agent) |
| [NewLife.Templates](https://github.com/NewLifeX/NewLife.Zero) | `dotnet new` 项目模板：控制台、Web、WebApi、服务、数据与物联网。 | [![NuGet](https://img.shields.io/nuget/dt/NewLife.Templates?style=flat-square&logo=nuget&label=downloads)](https://www.nuget.org/packages/NewLife.Templates) |
| **数据存储与消息** | | |
| [NewLife.Redis](https://github.com/NewLifeX/NewLife.Redis) | Redis 客户端：微秒级延迟、百万级吞吐，内置消息队列。 | [![NuGet](https://img.shields.io/nuget/dt/NewLife.Redis?style=flat-square&logo=nuget&label=downloads)](https://www.nuget.org/packages/NewLife.Redis) |
| [NewLife.RocketMQ](https://github.com/NewLifeX/NewLife.RocketMQ) | 纯托管 RocketMQ 客户端，支持 Apache RocketMQ 与阿里云消息队列。 | [![NuGet](https://img.shields.io/nuget/dt/NewLife.RocketMQ?style=flat-square&logo=nuget&label=downloads)](https://www.nuget.org/packages/NewLife.RocketMQ) |
| [NewLife.MQTT](https://github.com/NewLifeX/NewLife.MQTT) | MQTT 客户端与服务端，客户端支持阿里云物联网。 | [![NuGet](https://img.shields.io/nuget/dt/NewLife.MQTT?style=flat-square&logo=nuget&label=downloads)](https://www.nuget.org/packages/NewLife.MQTT) |
| [NewLife.NovaDb](https://github.com/NewLifeX/NewLife.NovaDb) | C# 混合数据库：嵌入/网络双模，关系型、时序、消息队列与 KV 于一体。 | [![NuGet](https://img.shields.io/nuget/dt/NewLife.NovaDb?style=flat-square&logo=nuget&label=downloads)](https://www.nuget.org/packages/NewLife.NovaDb) |
| [NewLife.MySql](https://github.com/NewLifeX/NewLife.MySql) | MySql 驱动与数据访问辅助层，基于 NewLife 数据栈实现。 | [![NuGet](https://img.shields.io/nuget/dt/NewLife.MySql?style=flat-square&logo=nuget&label=downloads)](https://www.nuget.org/packages/NewLife.MySql) |
| **Web、AI 与分布式** | | |
| [NewLife.Cube](https://github.com/NewLifeX/NewLife.Cube) | 魔方快速开发平台：用户权限、SSO、OAuth 服务端与 CRUD 脚手架。 | [![NuGet](https://img.shields.io/nuget/dt/NewLife.Cube?style=flat-square&logo=nuget&label=downloads)](https://www.nuget.org/packages/NewLife.Cube) |
| [Stardust](https://github.com/NewLifeX/Stardust) | 星尘分布式服务平台：节点管理、APM、配置中心、注册中心与发布中心。 | [![NuGet](https://img.shields.io/nuget/dt/NewLife.Stardust?style=flat-square&logo=nuget&label=downloads)](https://www.nuget.org/packages/NewLife.Stardust) |
| [AntJob](https://github.com/NewLifeX/AntJob) | 蚂蚁调度：分布式任务与实时数据平台，支持分片、重试与回溯补算。 | [![NuGet](https://img.shields.io/nuget/dt/NewLife.AntJob?style=flat-square&logo=nuget&label=downloads)](https://www.nuget.org/packages/NewLife.AntJob) |
| [NewLife.AI](https://github.com/NewLifeX/NewLife.AI) | 统一接入 46 家大模型服务商，附带开箱即用的 ChatAI Web 对话应用。 | [![NuGet](https://img.shields.io/nuget/dt/NewLife.AI?style=flat-square&logo=nuget&label=downloads)](https://www.nuget.org/packages/NewLife.AI) |
| **工业与物联网** | | |
| [NewLife.IoT](https://github.com/NewLifeX/NewLife.IoT) | IoT 标准库，定义物联网通信协议契约与设备抽象。 | [![NuGet](https://img.shields.io/nuget/dt/NewLife.IoT?style=flat-square&logo=nuget&label=downloads)](https://www.nuget.org/packages/NewLife.IoT) |
| [NewLife.Modbus](https://github.com/NewLifeX/NewLife.Modbus) | Modbus TCP/RTU/ASCII，基于 NewLife.IoT 实现。 | [![NuGet](https://img.shields.io/nuget/dt/NewLife.Modbus?style=flat-square&logo=nuget&label=downloads)](https://www.nuget.org/packages/NewLife.Modbus) |
| [NewLife.Siemens](https://github.com/NewLifeX/NewLife.Siemens) | 西门子 PLC 协议，基于 NewLife.IoT 实现。 | [![NuGet](https://img.shields.io/nuget/dt/NewLife.Siemens?style=flat-square&logo=nuget&label=downloads)](https://www.nuget.org/packages/NewLife.Siemens) |
| [NewLife.Omron](https://github.com/NewLifeX/NewLife.Omron) | 欧姆龙 PLC 协议，基于 NewLife.IoT 实现。 | [![NuGet](https://img.shields.io/nuget/dt/NewLife.Omron?style=flat-square&logo=nuget&label=downloads)](https://www.nuget.org/packages/NewLife.Omron) |
| [NewLife.Melsec](https://github.com/NewLifeX/NewLife.Melsec) | 三菱 Melsec PLC 协议，基于 NewLife.IoT 实现。 | [![NuGet](https://img.shields.io/nuget/dt/NewLife.Melsec?style=flat-square&logo=nuget&label=downloads)](https://www.nuget.org/packages/NewLife.Melsec) |
| [NewLife.OPC](https://github.com/NewLifeX/NewLife.OPC) | OPC 客户端，用于工业数据采集。 | — |
| [NewLife.BACnet](https://github.com/NewLifeX/NewLife.BACnet) | BACnet 楼宇自控协议栈。 | [![NuGet](https://img.shields.io/nuget/dt/NewLife.BACnet?style=flat-square&logo=nuget&label=downloads)](https://www.nuget.org/packages/NewLife.BACnet) |
| [ZeroIoT](https://github.com/NewLifeX/ZeroIoT) | 开源轻量级 IoT 平台，含网关与设备管理。 | — |
| **车联网** | | |
| [NewLife.JT808](https://github.com/NewLifeX/NewLife.JT808) | JT/T 808 车辆定位协议：70+ 消息体，覆盖 2011/2013/2019 三个版本。 | [![NuGet](https://img.shields.io/nuget/dt/NewLife.JT808?style=flat-square&logo=nuget&label=downloads)](https://www.nuget.org/packages/NewLife.JT808) |
| [NewLife.JT809](https://github.com/NewLifeX/NewLife.JT809) | JT/T 809 部标平台互联协议，面向运输管理部门。 | [![NuGet](https://img.shields.io/nuget/dt/NewLife.JT809?style=flat-square&logo=nuget&label=downloads)](https://www.nuget.org/packages/NewLife.JT809) |
| [NewLife.JT1078](https://github.com/NewLifeX/NewLife.JT1078) | JT/T 1078 车载视频：RTP 解析、FLV/fMP4 转封装与直播推送。 | [![NuGet](https://img.shields.io/nuget/dt/NewLife.JT1078?style=flat-square&logo=nuget&label=downloads)](https://www.nuget.org/packages/NewLife.JT1078) |
| **领域组件** | | |
| [NewLife.Office](https://github.com/NewLifeX/NewLife.Office) | 一个包覆盖 Excel、Word、PPT、PDF 等 15+ 文档格式。 | [![NuGet](https://img.shields.io/nuget/dt/NewLife.Office?style=flat-square&logo=nuget&label=downloads)](https://www.nuget.org/packages/NewLife.Office) |
| [NewLife.Audio](https://github.com/NewLifeX/NewLife.Audio) | 跨平台音频：编解码、设备 IO、DSP、容器与流式处理。 | [![NuGet](https://img.shields.io/nuget/dt/NewLife.Audio?style=flat-square&logo=nuget&label=downloads)](https://www.nuget.org/packages/NewLife.Audio) |
| [NewLife.Video](https://github.com/NewLifeX/NewLife.Video) | 视频编解码、封装格式、帧处理与流媒体。 | [![NuGet](https://img.shields.io/nuget/dt/NewLife.Video?style=flat-square&logo=nuget&label=downloads)](https://www.nuget.org/packages/NewLife.Video) |
| [NewLife.Map](https://github.com/NewLifeX/NewLife.Map) | 百度/高德/腾讯/天地图统一接口，支持地理编码与坐标转换。 | [![NuGet](https://img.shields.io/nuget/dt/NewLife.Map?style=flat-square&logo=nuget&label=downloads)](https://www.nuget.org/packages/NewLife.Map) |
| [NewLife.IP](https://github.com/NewLifeX/NewLife.IP) | IPv4 归属地：离线内存映射库 + 在线 API 双源。 | [![NuGet](https://img.shields.io/nuget/dt/NewLife.IP?style=flat-square&logo=nuget&label=downloads)](https://www.nuget.org/packages/NewLife.IP) |
| **工具与产品** | | |
| [CrazyCoder](https://github.com/NewLifeX/XCoder) | 码神工具：网络、串口、加解密、正则、Modbus 与 MQTT。 | [![NuGet](https://img.shields.io/nuget/dt/NewLife.XCoder?style=flat-square&logo=nuget&label=downloads)](https://www.nuget.org/packages/NewLife.XCoder) |
| [GitCandy](https://github.com/NewLifeX/GitCandy) | 自托管 Git 源代码管理系统。 | — |
| [LuckyClover](https://github.com/NewLifeX/LuckyClover) | 从低版本宿主启动时，自动安装匹配的 .NET 运行时。 | — |

> 徽章取各项目主包；主题包、扩展包与卫星包见对应 NuGet 页面。未在表中列出的 30 多个包（FTP、LoRa、Office 扩展、各类协议驱动等）见 [NewLifeX 组织](https://github.com/NewLifeX) 与 [newlifex.com](https://newlifex.com)。

## 新生命开发团队

<img src="https://newlifex.com/logo.png" width="48" align="right" alt="NewLife" />

新生命团队成立于 2002 年，二十余年专注 .NET 与物联网技术生态，主导开源项目近百个（生态版图收录 34 个主干项目），为电力、物流、医疗、工控、互联网等行业提供软硬件一体化解决方案；以“学无先后达者为师”为信条。

开源组件在 NuGet 累计下载 <!--nuget-total:start-->6.48M<!--nuget-total:end--> 次（全生态口径，截至 2026-10）。

网站：https://newlifex.com · 开源组织：https://github.com/NewLifeX

<details>
<summary>微信公众号：智能大石头（扫码关注）</summary>

![智能大石头](https://newlifex.com/stone.jpg)

</details>
