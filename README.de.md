# NewLife.IP - IP-Adressdatenbank und Geolokalisierung

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
🌐 [中文](Readme.MD) | [繁體中文](README.zh-TW.md) | [English](README.en.md) | [Español](README.es.md) | [日本語](README.ja.md) | [Français](README.fr.md) | [Português](README.pt.md) | [Русский](README.ru.md) | [한국어](README.ko.md) | [العربية](README.ar.md)
<!-- lang:end -->

> Diese Übersetzung kann hinter der chinesischen Version zurückliegen.

NewLife.IP bietet schnelle IP-Abfragen für IPv4/IPv6, weltweite Adressen und chinesische Verwaltungsregionen – Datenpakete inklusive.

## Installation

```bash
dotnet add package NewLife.IP
```

## Schnellstart

```csharp
var ip = new IpAddress("114.114.114.114");
var area = ip.GetArea();
```

## Funktionen

- Hoher Durchsatz und niedrige Latenz, im Produktivbetrieb erprobt
- Integrierte Funktionen und Daten; läuft ohne externe Dienste
- Plattformübergreifend: Windows, Linux, macOS und ARM64/LoongArch64

## Dokumentation

> 📖 Vollständige Dokumentation (Chinesisch): [Repository-Dokumente](Doc)

## Lizenz

MIT License © 2002-2026 NewLife-Team

[GitHub](https://github.com/NewLifeX/NewLife.IP) | [Website](https://newlifex.com)
