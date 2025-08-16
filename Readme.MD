# NewLife.IP - IP地址库

![GitHub top language](https://img.shields.io/github/languages/top/newlifex/NewLife.IP?logo=github)
![GitHub License](https://img.shields.io/github/license/newlifex/NewLife.IP?logo=github)
![Nuget Downloads](https://img.shields.io/nuget/dt/NewLife.IP?logo=nuget)
![Nuget](https://img.shields.io/nuget/v/NewLife.IP?logo=nuget)
![Nuget (with prereleases)](https://img.shields.io/nuget/vpre/NewLife.IP?label=dev%20nuget&logo=nuget)

高性能、跨平台的本地 IP 地址定位库（IPv4），支持将 IP 映射到（国家 / 省 / 市 / 区县）与运营商信息。通过内存映射文件 (MemoryMappedFile, MMF) 直接对压缩解包后的原始数据库文件进行二分检索，避免整表加载造成的托管内存压力，适合服务端与高并发场景。

源码： https://github.com/NewLifeX/NewLife.IP  
Nuget：`NewLife.IP`

---
## 特性 (Features)
- 多目标框架：net45 ~ net9.0 及 netstandard2.0 / 2.1，一包通用
- 零第三方运行时依赖（仅依赖 `NewLife.Core` 基础能力）
- 自动下载 & 更新 IP 数据文件（可配置数据目录与下载源）
- GZip 压缩数据库文件（`ip.gz`），首次使用自动解压到临时文件再用 MMF 打开
- 二分查找，O(log N) 复杂度；索引结构紧凑（7 字节 / 记录 + 变长字符串）
- 线程安全的并发查询（只读 + ThreadStatic 缓冲）
- 提供便捷扩展方法：`string.IPToAddress()` / `IPAddress.ToUInt32()` 等
- 可插入到 `NewLife.Net` 生态，通过 `IpResolver.Register()` 统一解析
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
```

---
## 核心类型说明
| 类型 | 作用 | 备注 |
| ---- | ---- | ---- |
| `Ip` | 高层封装：数据库文件定位、下载更新、对外查询 | 维护 `IpDatabase` 实例，懒加载 `Init()`
| `IpDatabase` | 低层二进制数据库访问（MMF + 二分） | 读取索引（`IndexInfo`）+ 地址字符串
| `IndexInfo` | 索引结构：Start/End/Offset | 7 字节索引区，Offset 指向数据块
| `IpHelper` | IP 与 UInt32 / 字符串互转扩展方法 | IPv4 Big-Endian 表示
| `IpResolver` | 实现 `NewLife.Net.IIPResolver` | 可注入生态组件统一解析

---
## 数据文件与更新机制
- 默认文件名：`ip.gz`，放置于 `Setting.Current.DataPath`（来自 `NewLife.Core` 配置体系）
- 若本地文件不存在、过小 (<3MB) 或早于内置阈值日期，则尝试从 `Setting.PluginServer` 下载
- 下载成功：覆盖旧文件，`Ip.Init()` 重新构建 MMF
- 支持用户自备同格式数据文件（兼容纯真 IPv4 库结构：索引区 + 记录区 + GB2312 文本）
- 读取时：
  1. 若为 `.gz` 则解压到临时文件
  2. 使用 `MemoryMappedFile.CreateFromFile` 方式只读访问
  3. 二分查找：通过起止 IP 定位记录 → 解析区域与地址字符串

---
## 查询流程 (简述)
1. 输入 IPv4 => 转为 `UInt32` (Big-Endian)
2. 在索引区做二分：`(start,end)` 覆盖范围即命中
3. 读取指向的数据段：按标记字节 (1 / 2) 判断是否重定向
4. 解析 GB2312 字节串，过滤保留字 `CZ88.NET`，返回 `(area, addr)`

---
## 性能与资源
- 索引二分：log2(N) 级；N≈数十万记录时仅 ~18 次比较
- MMF 避免一次性全部装入托管堆；仅在命中区域产生极少字符串分配
- ThreadStatic 缓冲区（64B）降低重复分配；GB2312 解码仅针对命中段
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
- 日志：所有关键事件走 `XTrace`；可接入星尘 (Stardust) 分布式追踪与日志汇聚

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
仓库包含 xUnit 测试工程 `XUnitTest`：
```
dotnet test
```
主要验证：
- IP/UInt32 互转正确性
- 常见真实 IP 解析的区域一致性
- 多线程并发解析稳定性

---
## 版本策略 / 更新
- 语义化版本：`MAJOR.MINOR.PATCH`，当前主版本为 2.x
- 包含多 TFM，保持 API 向后兼容；破坏性变更会先标记 `[Obsolete]`
- Release Notes（NuGet / GitHub）突出：性能 / 兼容 / 安全修复

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

## 新生命项目矩阵
各项目默认支持net9.0/netstandard2.1/netstandard2.0/net4.62/net4.5，旧版（2024.0801）支持net4.0/net2.0  

|                               项目                               | 年份  | 说明                                                                                        |
| :--------------------------------------------------------------: | :---: | ------------------------------------------------------------------------------------------- |
|                             基础组件                             |       | 支撑其它中间件以及产品项目                                                                  |
|          [NewLife.Core](https://github.com/NewLifeX/X)           | 2002  | 核心库，日志、配置、缓存、网络、序列化、APM性能追踪                                         |
|    [NewLife.XCode](https://github.com/NewLifeX/NewLife.XCode)    | 2005  | 大数据中间件，单表百亿级，MySql/SQLite/SqlServer/Oracle/PostgreSql/达梦，自动分表，读写分离 |
|      [NewLife.Net](https://github.com/NewLifeX/NewLife.Net)      | 2005  | 网络库，单机千万级吞吐率（2266万tps），单机百万级连接（400万Tcp长连接）                     |
| [NewLife.Remoting](https://github.com/NewLifeX/NewLife.Remoting) | 2011  | 协议通信库，提供CS应用通信框架，支持Http/RPC通信框架，高吞吐，物联网设备低开销易接入        |
|     [NewLife.Cube](https://github.com/NewLifeX/NewLife.Cube)     | 2010  | 魔方快速开发平台，集成了用户权限、SSO登录、OAuth服务端等，单表100亿级项目验证               |
|    [NewLife.Agent](https://github.com/NewLifeX/NewLife.Agent)    | 2008  | 服务管理组件，把应用安装成为操作系统守护进程，Windows服务、Linux的Systemd                   |
|     [NewLife.Zero](https://github.com/NewLifeX/NewLife.Zero)     | 2020  | Zero零代脚手架，基于NewLife组件生态的项目模板NewLife.Templates，Web、WebApi、Service        |
|                              中间件                              |       | 对接知名中间件平台                                                                          |
|    [NewLife.Redis](https://github.com/NewLifeX/NewLife.Redis)    | 2017  | Redis客户端，微秒级延迟，百万级吞吐，丰富的消息队列，百亿级数据量项目验证                   |
| [NewLife.RocketMQ](https://github.com/NewLifeX/NewLife.RocketMQ) | 2018  | RocketMQ纯托管客户端，支持Apache RocketMQ和阿里云消息队列，十亿级项目验                     |
|     [NewLife.MQTT](https://github.com/NewLifeX/NewLife.MQTT)     | 2019  | 物联网消息协议，MqttClient/MqttServer，客户端支持阿里云物联网                               |
|      [NewLife.IoT](https://github.com/NewLifeX/NewLife.IoT)      | 2022  | IoT标准库，定义物联网领域的各种通信协议标准规范                                             |
|   [NewLife.Modbus](https://github.com/NewLifeX/NewLife.Modbus)   | 2022  | ModbusTcp/ModbusRTU/ModbusASCII，基于IoT标准库实现，支持ZeroIoT平台和IoTEdge网关            |
|  [NewLife.Siemens](https://github.com/NewLifeX/NewLife.Siemens)  | 2022  | 西门子PLC协议，基于IoT标准库实现，支持IoT平台和IoTEdge                                      |
|      [NewLife.Map](https://github.com/NewLifeX/NewLife.Map)      | 2022  | 地图组件库，封装百度地图、高德地图、腾讯地图、天地图                                        |
|    [NewLife.Audio](https://github.com/NewLifeX/NewLife.Audio)    | 2023  | 音频编解码库，PCM/ADPCMA/G711A/G722U/WAV/AAC                                                |
|                             产品平台                             |       | 产品平台级，编译部署即用，个性化自定义                                                      |
|         [Stardust](https://github.com/NewLifeX/Stardust)         | 2018  | 星尘，分布式服务平台，节点管理、APM监控中心、配置中心、注册中心、发布中心                   |
|           [AntJob](https://github.com/NewLifeX/AntJob)           | 2019  | 蚂蚁调度，分布式大数据计算平台（实时/离线），蚂蚁搬家分片思想，万亿级数据量项目验证         |
|      [NewLife.ERP](https://github.com/NewLifeX/NewLife.ERP)      | 2021  | 企业ERP，产品管理、客户管理、销售管理、供应商管理                                           |
|         [CrazyCoder](https://github.com/NewLifeX/XCoder)         | 2006  | 码神工具，众多开发者工具，网络、串口、加解密、正则表达式、Modbus、MQTT                      |
|           [EasyIO](https://github.com/NewLifeX/EasyIO)           | 2023  | 简易文件存储，支持分布式系统中文件集中存储。                                                |
|           [XProxy](https://github.com/NewLifeX/XProxy)           | 2005  | 产品级反向代理，NAT代理、Http代理                                                           |
|        [HttpMeter](https://github.com/NewLifeX/HttpMeter)        | 2022  | Http压力测试工具                                                                            |
|         [GitCandy](https://github.com/NewLifeX/GitCandy)         | 2015  | Git源代码管理系统                                                                           |
|          [SmartOS](https://github.com/NewLifeX/SmartOS)          | 2014  | 嵌入式操作系统，完全独立自主，支持ARM Cortex-M芯片架构                                      |
|          [SmartA2](https://github.com/NewLifeX/SmartA2)          | 2019  | 嵌入式工业计算机，物联网边缘网关，高性能.NET8主机，应用于工业、农业、交通、医疗             |
|                          FIoT物联网平台                          | 2020  | 物联网整体解决方案，建筑、环保、农业，软硬件及大数据分析一体化，单机十万级点位项目验证      |
|                        UWB高精度室内定位                         | 2020  | 厘米级（10~20cm）高精度室内定位，软硬件一体化，与其它系统联动，大型展厅项目验证             |



## 新生命开发团队
![XCode](https://newlifex.com/logo.png)  

新生命团队（NewLife）成立于2002年，是新时代物联网行业解决方案提供者，致力于提供软硬件应用方案咨询、系统架构规划与开发服务。  
团队主导的80多个开源项目已被广泛应用于各行业，Nuget累计下载量高达400余万次。  
团队开发的大数据中间件NewLife.XCode、蚂蚁调度计算平台AntJob、星尘分布式平台Stardust、缓存队列组件NewLife.Redis以及物联网平台FIoT，均成功应用于电力、高校、互联网、电信、交通、物流、工控、医疗、文博等行业，为客户提供了大量先进、可靠、安全、高质量、易扩展的产品和系统集成服务。  

我们将不断通过服务的持续改进，成为客户长期信赖的合作伙伴，通过不断的创新和发展，成为国内优秀的IoT服务供应商。  

`新生命团队始于2002年，部分开源项目具有20年以上漫长历史，源码库保留有2010年以来所有修改记录`  
网站：https://newlifex.com  
开源：https://github.com/newlifex  
QQ群：1600800/1600838  
微信公众号：  
![智能大石头](https://newlifex.com/stone.jpg)  
