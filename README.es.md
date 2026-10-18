# NewLife.IP - Base de datos y geolocalización de IP

![GitHub License](https://img.shields.io/github/license/NewLifeX/NewLife.IP?logo=github)
![NuGet Downloads](https://img.shields.io/nuget/dt/NewLife.IP?logo=nuget)
![NuGet](https://img.shields.io/nuget/v/NewLife.IP?logo=nuget)
![NuGet prerelease](https://img.shields.io/nuget/vpre/NewLife.IP?label=dev%20nuget&logo=nuget)
[![test](https://github.com/NewLifeX/NewLife.IP/actions/workflows/test.yml/badge.svg?branch=master)](https://github.com/NewLifeX/NewLife.IP/actions/workflows/test.yml)
![platform](https://img.shields.io/badge/platform-Windows%20%7C%20Linux%20%7C%20macOS-blue)
[![contributors](https://img.shields.io/github/contributors/NewLifeX/NewLife.IP?logo=github)](https://github.com/NewLifeX/NewLife.IP/graphs/contributors)

<!-- lang:start -->
🌐 [中文](Readme.MD) | [繁體中文](README.zh-TW.md) | [English](README.en.md) | [Deutsch](README.de.md) | [日本語](README.ja.md) | [Français](README.fr.md) | [Português](README.pt.md) | [Русский](README.ru.md) | [한국어](README.ko.md) | [العربية](README.ar.md)
<!-- lang:end -->

> Esta traducción puede estar desactualizada; consulte la versión en chino.

NewLife.IP ofrece consultas de IP de alto rendimiento para IPv4/IPv6, direcciones globales y regiones administrativas chinas, con datos incluidos.

## Instalación

```bash
dotnet add package NewLife.IP
```

## Inicio rápido

```csharp
var ip = new IpAddress("114.114.114.114");
var area = ip.GetArea();
```

## Características

- Alto rendimiento y baja latencia, probado a escala de producción
- Capacidades y datos integrados; funciona sin servicios externos
- Multiplataforma: Windows, Linux, macOS y ARM64/LoongArch64

## Documentación

> 📖 Documentación completa (en chino): [documentos del repositorio](Doc)

## Licencia

MIT License © 2002-2026 Equipo NewLife

[GitHub](https://github.com/NewLifeX/NewLife.IP) | [Website](https://newlifex.com)
