# NewLife.IP - IP 位址庫與地理定位

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
🌐 [English](README.md) | [简体中文](README.zh-CN.md) | [Español](README.es.md) | [Deutsch](README.de.md) | [日本語](README.ja.md) | [Français](README.fr.md) | [Português](README.pt.md) | [Русский](README.ru.md) | [한국어](README.ko.md) | [العربية](README.ar.md)
<!-- lang:end -->

> 本翻譯可能落後，以 [English README](README.md) 為準。

NewLife.IP 提供高效能 IP 位址庫查詢，支援 IPv4/IPv6、國內外位址與中國行政區劃定位，內建資料檔即裝即用。

## 安裝

```bash
dotnet add package NewLife.IP
```

## 快速開始

```csharp
var ip = new IpAddress("114.114.114.114");
var area = ip.GetArea();
```

## 核心特性

- 高吞吐、低延遲，已在生產環境大規模驗證
- 能力與資料內建，無需外部服務即可執行
- 跨平台支援 Windows、Linux、macOS 與 ARM64/LoongArch64

## 文件

> 📖 完整文件（中文）見 [儲存庫文件](Doc)

## 授權

MIT License © 2002-2026 新生命開發團隊

[GitHub](https://github.com/NewLifeX/NewLife.IP) | [Website](https://newlifex.com)
