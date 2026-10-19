# NewLife.IP - قاعدة عناوين IP وتحديد الموقع

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
🌐 [English](README.md) | [简体中文](README.zh-CN.md) | [繁體中文](README.zh-TW.md) | [Español](README.es.md) | [Deutsch](README.de.md) | [日本語](README.ja.md) | [Français](README.fr.md) | [Português](README.pt.md) | [Русский](README.ru.md) | [한국어](README.ko.md)
<!-- lang:end -->

> قد تتأخر هذه الترجمة عن [النسخة الإنجليزية](README.md).

توفر NewLife.IP استعلامات IP عالية الأداء لـ IPv4/IPv6 والعناوين العالمية والمناطق الإدارية الصينية، مع ملفات بيانات مضمّنة.

## التثبيت

```bash
dotnet add package NewLife.IP
```

## البدء السريع

```csharp
var ip = new IpAddress("114.114.114.114");
var area = ip.GetArea();
```

## الميزات الرئيسية

- إنتاجية عالية وزمن استجابة منخفض، مُثبت على نطاق الإنتاج
- إمكانات وبيانات مضمّنة تعمل دون خدمات خارجية
- متعدد المنصات: Windows وLinux وmacOS وARM64/LoongArch64

## التوثيق

> 📖 التوثيق الكامل (بالصينية): [مستندات المستودع](Doc)

## الترخيص

MIT License © 2002-2026 فريق NewLife

[GitHub](https://github.com/NewLifeX/NewLife.IP) | [Website](https://newlifex.com)
