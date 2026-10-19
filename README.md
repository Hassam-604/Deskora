# Deskora

> **A cross-platform desktop personalization studio for wallpapers, icons, layouts, themes, widgets, and live backgrounds â€” built incrementally with a focus on performance, safety, and usability.**

---

## Overview

Deskora is an open desktop personalization studio designed to unify desktop customization on Windows (with planned cross-platform support). It is structured to provide rich customization while strictly adhering to safety principles:

- **Isolated System Integrations:** Platform-specific Win32 and COM integrations are kept strictly separate from the Avalonia UI presentation layer.
- **Safe Rollback First:** Every system modification will be backed by a snapshot and a verified restoration mechanism.
- **No Background Bloat:** Zero unapproved background services, no automatic startup without user consent, and strict CPU/RAM limits.
- **No Fake Controls:** Incomplete features are explicitly identified as roadmap items rather than non-functional buttons.
- **Privacy First:** Zero telemetry, no hidden tracking, and offline-first asset management.

---

## Current Status: Milestone 0.4.0 â€” Icon Customization

Milestone `0.4.0` expands Deskora with a safe, reversible Windows desktop icon customization studio. Users can customize desktop shortcuts (`.lnk`) and desktop folders without modifying PE binary files or system-protected files, backed by automatic baseline snapshots and 1-click restoration.

### Features in 0.4.0
- **Desktop Icon Studio & 1,000+ Built-in Library (0.4.0):**
  - **Native Windows Icon Library (1,400+ Icons):** Instant access to all high-DPI icons embedded in imageres.dll, shell32.dll, and ddores.dll with categorized filters (Folders, Devices, System, Media, Apps) and zero disk overhead.
  - **Curated App & Aesthetic Folder Packs:** Pre-bundled modern app icons (Chrome, Discord, Steam, Spotify, VS Code, GitHub, Terminal, Python, Blender, Games) and aesthetic neon/pastel folders.
  - **Reversible Shortcut Customization:** Customizes `.lnk` shortcuts using official Windows Shell COM interfaces (`IShellLinkW`, `IPersistFile`).
  - **Desktop Folder Customization:** Customizes desktop directory icons via standard hidden `desktop.ini` descriptors with `FileAttributes.ReadOnly` directory flag.
  - **Zero-Allocation Icon Validation:** `IconValidator` inspects `.ico` magic bytes, directory headers, dimensions, and offsets without memory allocation, and validates container libraries (`.exe`, `.dll`).
  - **Strict Safety Guarantee:** Never modifies target application binaries (`.exe`), PE structures, or protected system files.
  - **Automatic Baseline Snapshots:** Automatically records the original Windows icon location before any change, stored atomically in `%LocalAppData%\Deskora\icon_preferences.json`.
  - **1-Click Rollback & Global Reset:** Restore any modified icon back to its original Windows state with 1 click, or reset all desktop icons at once.
  - **Shell Redraw Notification:** Notifies Windows Explorer of icon updates via Win32 `SHChangeNotify(SHCNE_ASSOCCHANGED, SHCNF_FLUSH)` without terminating `explorer.exe`.
  - **Interactive Desktop Icon UI:** Live search filter, folder/shortcut indicators, custom icon file picker, status badges, and validation alerts.
- **Curated Downloadable Gallery (0.3.0):**
  - **Multi-Source Online Providers:** Simultaneously searches Wallhaven 4K and the Nekos.best Art Archive with complete artist and source attribution.
  - **Offline-First Resilience:** Gracefully falls back to a bundled local JSON catalog (`catalog.json`) when offline.
  - **Safe Sandboxed Download Manager:** Enforces strict HTTPS URLs, allowed image formats, 50 MB safety size quotas, streaming progress, and SHA-256 integrity verification.
  - **Interactive Search & Filtering:** Instant in-memory filtering combined with 400ms debounced online keyword querying and category pills.
  - **Asset Detail Inspector & 1-Click Application:** Detailed side drawer showing resolution, aspect ratio, author, license, and storage statistics with 1-click desktop application.
- **Local Wallpaper Studio (0.2.0):**
  - **Gallery & Folder Scanning:** Scan any local folder or pick individual images (PNG, JPG, BMP, WebP).
  - **Zero-Allocation Image Header Decoding:** `ImageHeaderReader` inspects dimensions and aspect ratios instantly.
  - **Multi-Monitor Configuration:** Detects connected displays via Windows `IDesktopWallpaper` COM API; configure wallpapers per display or across all monitors.
  - **1-Click Safe Rollback:** Automatically captures desktop wallpaper snapshots before changes.
- **Polished Desktop Shell (0.1.0):**
  - **Dynamic Navigation:** Page switching across 9 modules.
  - **Live Theme Engine:** Real-time theme switching between System Default, Dark Mode, and Light Mode.
  - **Alert & Error System:** Top notification banner supporting Info, Warning, and Error alerts.
- **Automated Test Suite:** 78 automated unit tests covering navigation, themes, image decoders, gallery providers, download pipeline, icon validation, icon preferences persistence, and ViewModel orchestration.

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
â”œâ”€â”€ src/
â”‚   â”œâ”€â”€ Deskora.Core/           # Core domain models, services, and ViewModels
â”‚   â”‚   â”œâ”€â”€ Models/             # Navigation, theme, notification, and feature models
â”‚   â”‚   â”œâ”€â”€ Services/           # INavigationService, IThemeService, IErrorService
â”‚   â”‚   â””â”€â”€ ViewModels/         # MVVM ViewModels for all pages and main window
â”‚   â””â”€â”€ Deskora.App/            # Avalonia desktop presentation layer
â”‚       â”œâ”€â”€ Converters/         # Value converters (icon geometry resolver)
â”‚       â”œâ”€â”€ Styles/             # SVG vector icons and design system styling
â”‚       â”œâ”€â”€ Views/              # Avalonia XAML views and code-behind
â”‚       â”œâ”€â”€ App.axaml           # Application entry and resource merged dictionaries
â”‚       â””â”€â”€ Program.cs          # Desktop application entry point
â”œâ”€â”€ tests/
â”‚   â””â”€â”€ Deskora.Tests/          # xUnit automated test project
â”œâ”€â”€ docs/
â”‚   â”œâ”€â”€ ROADMAP.md              # Planned release milestones (0.1.0 to 1.0.0)
â”‚   â””â”€â”€ DEVELOPMENT.md          # Developer environment and contribution guide
â”œâ”€â”€ .gitignore                  # Standard .NET / Avalonia ignore patterns
â”œâ”€â”€ Deskora.sln                 # Solution file
â””â”€â”€ README.md                   # This documentation
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

