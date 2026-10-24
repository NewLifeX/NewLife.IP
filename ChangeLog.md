# NewLife.IP 版本更新记录

## v2.6.2026.1024 (2026-10-24)

### 运行时多语言
- **31 种语言词表**：接入 NewLife.Core 12 本地化体系，词表由源生成器注册，`Localization.Culture` 与 `LogCulture` 可分别控制界面与日志语言，未注册语言回退英文
- **日志本地化**：`Ip` / `IpDatabase` / `IpCsv` / `OnlineIp` 统一接入 `ILog` 与 `LogPrefix`，消息先翻译再拼前缀
- **多语言文档**：新增 11 种语言 README，CI 校验 32 个 Locales 词表的键集合一致性

### 跨平台与移动端
- **Android/iOS 冒烟**：新增 `Samples/MobileSmoke` 工程，在模拟器上验证程序集加载、IP 转换、多语言与 CSV 转换
- **多架构测试矩阵**：单元测试覆盖 win-x64 / linux-x64 / linux-arm64 / linux-loongarch64（QEMU 用户态仿真）

### AOT 与构建约束
- **NativeAOT 冒烟**：`AotSmoke` 的 21 个场景在 win-x64 / linux-x64 / linux-arm64 原生发布运行
- **分析器零告警**：net8.0+ 目标常驻 `IsAotCompatible`，CI 以 `-warnaserror` 守住 trim/AOT 告警
- **工程现代化**：解决方案迁移到 `.slnx` 格式，升级 Microsoft.SourceLink.GitHub 修复 NU1902 漏洞告警

### 测试与质量
- **xUnit v3 迁移**：测试项目迁移到 xUnit v3 + Microsoft.Testing.Platform，用例扩充到 111 个
- **[fix]** 修复 IP 落在索引区间之外时的查询死循环
- **[fix]** 修复 Linux 上读取尾部记录越界异常
- **[fix]** 补齐 netstandard 运行时代码页注册，修正旧式区域别名

### 依赖升级
- 升级 NewLife.Core 到 12.0.2026.1024

---

## v2.5.2026.0804 (2026-08-04)

### 在线 IP 对接
- **离线/在线双数据源**：新增 `OnlineIp` 在线解析器，`IpResolver.Online` 一键切换，默认离线保持本地查询
- **国内厂商对接**：支持腾讯 / 高德 / 百度 IP 定位（需免费 Key），响应多格式自动解析，中国境内省市区县高精度
- **在线缓存与容错**：查询结果 MemoryCache 缓存，网络失败 / 限流返回空不阻断业务

### 多数据源适配
- **DB-IP CSV 转换**：新增 `IpCsv` 转换器，将 DB-IP 免费 CSV 数据转换为纯真兼容数据库文件

### 测试与质量
- **测试全覆盖**：15 个功能三维（实现/测试/注释）全部完成，42 个自动化测试通过

### Bug 修复
- **[fix]** 修复 `IpDatabase.ReadString` 未跳过 0 终止符导致普通记录地址读取为空
- **[fix]** 修复 `IpDatabase.OnDispose` 未释放 MMF 视图导致 gz 临时文件无法删除

### 文档与协作
- 新增需求文档、功能清单、架构设计、竞品分析文档体系

---
