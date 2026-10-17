# Deskora

> **A cross-platform desktop personalization studio for wallpapers, icons, layouts, themes, widgets, and live backgrounds — built incrementally with a focus on performance, safety, and usability.**

---

## Overview

Deskora is an open desktop personalization studio designed to unify desktop customization on Windows (with planned cross-platform support). It is structured to provide rich customization while strictly adhering to safety principles:

- **Isolated System Integrations:** Platform-specific Win32 and COM integrations are kept strictly separate from the Avalonia UI presentation layer.
- **Safe Rollback First:** Every system modification will be backed by a snapshot and a verified restoration mechanism.
- **No Background Bloat:** Zero unapproved background services, no automatic startup without user consent, and strict CPU/RAM limits.
- **No Fake Controls:** Incomplete features are explicitly identified as roadmap items rather than non-functional buttons.
- **Privacy First:** Zero telemetry, no hidden tracking, and offline-first asset management.

---

## Current Status: Milestone 0.3.0 — Curated Downloadable Gallery

Milestone `0.3.0` expands Deskora with an online-connected, offline-resilient wallpaper gallery featuring thousands of vetted 4K desktop wallpapers, a multi-source provider architecture, safe sandboxed downloads, and 1-click desktop customization.

### Features in 0.3.0
- **Curated Downloadable Gallery:**
  - **Multi-Source Online Providers:** Simultaneously searches Wallhaven 4K (filtered for 16:9 & 16:10 desktop ratios in 1080p, 1440p, and 4K UHD) and the Nekos.best Art Archive with complete artist and source attribution.
  - **Offline-First Resilience:** Gracefully falls back to a bundled, approved local JSON catalog (`catalog.json`) when internet connectivity is unavailable.
  - **Safe Sandboxed Download Manager:** Enforces strict HTTPS URLs, allowed image formats (`.jpg`, `.jpeg`, `.png`, `.webp`, `.bmp`), and a 50 MB safety size quota. Downloads stream atomically to temporary files with cancellation, error cleanup, and optional SHA-256 integrity verification.
  - **Interactive Search & Filtering:** Instant in-memory filtering combined with 400ms debounced online keyword querying. Category pills for All, Nature, Space, Abstract, Architecture, Minimal, and Anime.
  - **Asset Detail Inspector & 1-Click Application:** Detailed side drawer showing resolution, aspect ratio, author, license, and storage statistics with 1-click application directly to your desktop.
  - **Thread-Safe MVVM Architecture:** Synchronized UI thread dispatching eliminates cross-thread collection mutation issues during rapid typing.
- **Local Wallpaper Studio (0.2.0):**
  - **Gallery & Folder Scanning:** Scan any local folder or pick individual images (PNG, JPG, BMP, WebP) with background processing.
  - **Zero-Allocation Image Header Decoding:** `ImageHeaderReader` inspects dimensions and aspect ratios instantly without loading multi-megabyte pixel buffers into memory.
  - **High-Performance Thumbnail Caching:** Fluid 60fps scrolling backed by memory-cached downscaled thumbnails.
  - **Multi-Monitor Configuration:** Detects connected displays via Windows `IDesktopWallpaper` COM API; configure wallpapers per display or across all monitors.
  - **Wallpaper Fit Positions:** Full support for Windows fit options: Fill, Fit, Stretch, Center, Tile, and Span.
  - **1-Click Safe Rollback:** Automatically captures desktop wallpaper snapshots before any modifications, enabling instant restore.
  - **Favorites & Recent History:** Favorite wallpapers and track recently applied images across sessions via local JSON preferences.
- **Polished Desktop Shell:** Fixed, predictable layout featuring modern sidebar navigation, version branding, and dynamic status footer.
- **Dynamic Navigation:** Complete page switching across 9 modules (Home, Wallpapers, Live Wallpapers, Icons, Layouts, Themes, Widgets, Settings, About).
- **Live Theme Engine:** Real-time theme switching between System Default, Dark Mode, and Light Mode via Avalonia `RequestedThemeVariant`.
- **Alert & Error System:** Top notification banner supporting Info, Warning, and Error alerts with user dismissal.
- **Automated Test Suite:** 57 automated unit tests covering navigation, theme switching, error notifications, fast image header parsing, local wallpaper workflows, multi-source catalog parsing, download integrity verification, and ViewModel orchestration.

---

## Technology Stack

| Layer | Technology | Details |
|---|---|---|
| **Runtime** | .NET 8.0 (LTS) | Cross-platform modern C# runtime |
| **Language** | C# 12 | Strong typing, records, and pattern matching |
| **UI Framework** | Avalonia UI 11.2.5 | Modern cross-platform XAML UI toolkit |
| **Architecture Pattern** | MVVM | Clean separation via `CommunityToolkit.Mvvm` |
| **Testing** | xUnit & .NET Test SDK | Automated unit test suite |

---

## Solution Structure

```
Deskora/
├── src/
│   ├── Deskora.Core/           # Core domain models, services, and ViewModels
│   │   ├── Models/             # Navigation, theme, notification, and feature models
│   │   ├── Services/           # INavigationService, IThemeService, IErrorService
│   │   └── ViewModels/         # MVVM ViewModels for all pages and main window
│   └── Deskora.App/            # Avalonia desktop presentation layer
│       ├── Converters/         # Value converters (icon geometry resolver)
│       ├── Styles/             # SVG vector icons and design system styling
│       ├── Views/              # Avalonia XAML views and code-behind
│       ├── App.axaml           # Application entry and resource merged dictionaries
│       └── Program.cs          # Desktop application entry point
├── tests/
│   └── Deskora.Tests/          # xUnit automated test project
├── docs/
│   ├── ROADMAP.md              # Planned release milestones (0.1.0 to 1.0.0)
│   └── DEVELOPMENT.md          # Developer environment and contribution guide
├── .gitignore                  # Standard .NET / Avalonia ignore patterns
├── Deskora.sln                 # Solution file
└── README.md                   # This documentation
```

---

## Getting Started

### Prerequisites
- [.NET 8.0 SDK](https://dotnet.microsoft.com/download/dotnet/8.0) or higher.
- Git.

### Building and Testing

1. **Clone the repository:**
   ```bash
   git clone <repository-url>
   cd Deskora
   ```

2. **Restore dependencies:**
   ```bash
   dotnet restore
   ```

3. **Build the solution:**
   ```bash
   dotnet build
   ```

4. **Run the automated tests:**
   ```bash
   dotnet test
   ```

5. **Launch Deskora:**
   ```bash
   dotnet run --project src/Deskora.App
   ```

---

## License

*License to be announced by the Product Owner. All rights reserved.*
