<div align="center">

# 🎥 RecordFlow

### A clean, dependable screen recorder built with .NET MAUI

[![.NET MAUI](https://img.shields.io/badge/.NET%20MAUI-.NET%209-512BD4?style=for-the-badge&logo=dotnet&logoColor=white)](https://dotnet.microsoft.com/apps/maui)
[![Android](https://img.shields.io/badge/Android-3DDC84?style=for-the-badge&logo=android&logoColor=white)](#-supported-platforms)
[![iOS](https://img.shields.io/badge/iOS-000000?style=for-the-badge&logo=apple&logoColor=white)](#-supported-platforms)
[![Windows](https://img.shields.io/badge/Windows-0078D4?style=for-the-badge)](#-supported-platforms)
[![MVVM Toolkit](https://img.shields.io/badge/CommunityToolkit-MVVM-7B3FE4?style=for-the-badge)](https://learn.microsoft.com/dotnet/communitytoolkit/mvvm/)

</div>

---

## ✨ Features

- ⏺️ **One-tap recording** — start and stop screen capture from a single, focused screen
- ⏱️ **Live timer** — see the elapsed `hh:mm:ss` while you record
- 🏷️ **Name your recordings** — choose a file name before you start
- 📁 **Smart save locations** — recordings land in the right place per platform (e.g. *Videos* on Windows, the gallery on mobile) in a dedicated *RecordFlow* folder
- 🔐 **Permission flow done right** — clear rationale dialogs and graceful fallbacks when permissions are denied
- 🔔 **Notifications** — know when a recording is saved
- ℹ️ **Android audio guidance** — up-front notice about Android 10+ internal-audio limitations so there are no surprises
- 🔄 **Update check** — built-in helper surfaces new versions

## 📱 Supported Platforms

| Platform | Target |
|---|---|
| Android | `net9.0-android` |
| iOS | `net9.0-ios` |
| macOS (Mac Catalyst) | `net9.0-maccatalyst` |
| Windows | `net9.0-windows10.0.19041.0` |

> Screen recording support depends on the platform's capabilities; the app checks `IsSupported` at runtime and tells the user if a device can't record.

## 🛠️ Tech Stack

- **Framework:** .NET MAUI on .NET 9
- **Recording:** [Plugin.Maui.ScreenRecording](https://github.com/jfversluis/Plugin.Maui.ScreenRecording)
- **Architecture:** CommunityToolkit.Mvvm messaging, helper services (`PermissionsHelper`, `SaveHelper`, `NotificationHelper`, `UpdateHelper`)

## ⚡ Getting Started

**Prerequisites:** Visual Studio 2022 with the **.NET Multi-platform App UI development** workload.

```bash
git clone https://github.com/smuhammadbilalh/RecordFlow.git
cd RecordFlow
dotnet restore
dotnet build -t:Run -f net9.0-android      # or net9.0-windows10.0.19041.0
```

## 🗂️ Project Structure

```
RecordFlow/
├── MainPage.xaml(.cs)      # Recorder UI, timer and state
├── DeveloperPage.xaml(.cs) # About the developer
├── PermissionsHelper.cs    # Runtime permission checks & rationale
├── SaveHelper.cs           # Per-platform output paths
├── NotificationHelper.cs   # Local notifications
├── UpdateHelper.cs         # Version checks
└── Platforms/              # Android, iOS, MacCatalyst, Windows specifics
```

## 🗺️ Roadmap

- [ ] Pause / resume
- [ ] Microphone & internal audio options per platform
- [ ] Quality / resolution presets
- [ ] In-app recordings gallery

---

## 👨‍💻 Author

<div align="center">

**Muhammad Bilal** — IT Specialist & App Developer · Gujrat, Pakistan

[![GitHub](https://img.shields.io/badge/GitHub-smuhammadbilalh-181717?style=for-the-badge&logo=github)](https://github.com/smuhammadbilalh)
[![LinkedIn](https://img.shields.io/badge/LinkedIn-muhammadbilaltech-0A66C2?style=for-the-badge)](https://www.linkedin.com/in/muhammadbilaltech)
[![Email](https://img.shields.io/badge/Email-smuhammadbilalh%40gmail.com-EA4335?style=for-the-badge&logo=gmail&logoColor=white)](mailto:smuhammadbilalh@gmail.com)

⭐ If this project helped you, please consider giving it a star!

</div>
