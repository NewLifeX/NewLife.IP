# NewLife.IP - Base d'adresses IP et géolocalisation

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
🌐 [中文](Readme.MD) | [繁體中文](README.zh-TW.md) | [English](README.en.md) | [Español](README.es.md) | [Deutsch](README.de.md) | [日本語](README.ja.md) | [Português](README.pt.md) | [Русский](README.ru.md) | [한국어](README.ko.md) | [العربية](README.ar.md)
<!-- lang:end -->

> Cette traduction peut être en retard sur la version chinoise.

NewLife.IP propose des recherches IP hautes performances pour IPv4/IPv6, adresses mondiales et régions administratives chinoises, données incluses.

## Installation

```bash
dotnet add package NewLife.IP
```

## Démarrage rapide

```csharp
var ip = new IpAddress("114.114.114.114");
var area = ip.GetArea();
```

## Fonctionnalités

- Haut débit et faible latence, éprouvés à l'échelle de la production
- Fonctionnalités et données intégrées ; fonctionne sans service externe
- Multiplateforme : Windows, Linux, macOS et ARM64/LoongArch64

## Documentation

> 📖 Documentation complète (chinois) : [documents du dépôt](Doc)

## Licence

MIT License © 2002-2026 Équipe NewLife

[GitHub](https://github.com/NewLifeX/NewLife.IP) | [Website](https://newlifex.com)
