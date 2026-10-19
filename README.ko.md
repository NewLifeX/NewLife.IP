# NewLife.IP - IP 주소 데이터베이스 및 위치 정보

![GitHub License](https://img.shields.io/github/license/NewLifeX/NewLife.IP?logo=github)
![NuGet Downloads](https://img.shields.io/nuget/dt/NewLife.IP?logo=nuget)
![NuGet](https://img.shields.io/nuget/v/NewLife.IP?logo=nuget)
![NuGet prerelease](https://img.shields.io/nuget/vpre/NewLife.IP?label=dev%20nuget&logo=nuget)
[![test](https://github.com/NewLifeX/NewLife.IP/actions/workflows/test.yml/badge.svg?branch=master)](https://github.com/NewLifeX/NewLife.IP/actions/workflows/test.yml)
[![locales](https://img.shields.io/badge/locales-11-2ea44f?logo=dotnet)](Doc/多语言本地化.md)
![platform](https://img.shields.io/badge/platform-Windows%20%7C%20Linux%20%7C%20macOS%20%7C%20Android%20%7C%20iOS-blue)
![arch](https://img.shields.io/badge/arch-x64%20%7C%20arm64%20%7C%20loongarch64-orange)
[![contributors](https://img.shields.io/github/contributors/NewLifeX/NewLife.IP?logo=github)](https://github.com/NewLifeX/NewLife.IP/graphs/contributors)

<!-- lang:start -->
🌐 [中文](Readme.MD) | [繁體中文](README.zh-TW.md) | [English](README.en.md) | [Español](README.es.md) | [Deutsch](README.de.md) | [日本語](README.ja.md) | [Français](README.fr.md) | [Português](README.pt.md) | [Русский](README.ru.md) | [العربية](README.ar.md)
<!-- lang:end -->

> 이 번역은 중국어 버전보다 늦을 수 있습니다.

NewLife.IP는 IPv4/IPv6, 전 세계 주소, 중국 행정 구역에 대한 고성능 IP 조회를 제공하며 데이터 파일을 내장합니다.

## 설치

```bash
dotnet add package NewLife.IP
```

## 빠른 시작

```csharp
var ip = new IpAddress("114.114.114.114");
var area = ip.GetArea();
```

## 주요 기능

- 높은 처리량과 낮은 지연, 대규모 운영 환경에서 검증됨
- 기능과 데이터 내장, 외부 서비스 없이 동작
- 크로스 플랫폼: Windows, Linux, macOS, ARM64/LoongArch64

## 문서

> 📖 전체 문서(중국어): [저장소 문서](Doc)

## 라이선스

MIT License © 2002-2026 NewLife 팀

[GitHub](https://github.com/NewLifeX/NewLife.IP) | [Website](https://newlifex.com)
