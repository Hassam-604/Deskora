# Deskora â€” Product Roadmap

This document outlines the sequential release milestones for **Deskora**. Development is strictly incremental: features are designed, tested, verified, and approved by the Product Owner before advancing to subsequent milestones.

---

## Milestone Overview

| Version | Milestone Objective | Status | Focus |
|---|---|---|---|
| **0.1.0** | **Foundation and App Shell** | **Completed** | Solution architecture, navigation shell, design system, theme engine, tests |
| **0.2.0** | **Local Wallpapers** | **Completed** | Local folder browsing, multi-monitor configuration, aspect ratio fit, undo/restore |
| **0.3.0** | **Curated Downloadable Gallery** | **Completed** | Approved asset catalog, previews, licensing metadata, download manager, offline storage |
| **0.4.0** | **Icon Customization** | **Completed** | Desktop shortcut icons, folder icons, Windows shell cache refresh, backup points, safe reset |
| **0.5.0** | **Desktop Layouts and Presets** | Planned | SysListView32 icon coordinate capture, multi-monitor resolution restoration, named profiles |
| **0.6.0** | **Live Wallpaper Proof of Concept** | Planned | WorkerW canvas hooking, video playback engine, auto-pause on fullscreen, crash isolation |
| **0.7.0** | **In-App and Floating Widgets** | Planned | Memory-budgeted desktop widgets (clocks, stats), WS_EX_TOOLWINDOW layering |
| **0.8.0** | **Themes and Integration** | Planned | DWM accent color matching, coordinated wallpaper/widget/icon bundles |
| **0.9.0** | **Beta Hardening and Installer** | Planned | End-to-end stress testing, crash telemetry reporting, MSIX/InnoSetup installer |
| **1.0.0** | **Stable Release** | Planned | Full production documentation, finalized licensing, release packaging |

---

## Milestone Details

### 0.1.0 â€” Foundation and App Shell
- Establish solution structure (`Deskora.Core`, `Deskora.App`, `Deskora.Tests`).
- Implement navigation shell with responsive resizing.
- Create 9 modules (Home, Wallpapers, Live Wallpapers, Icons, Layouts, Themes, Widgets, Settings, About).
- Implement active theme engine (System Default, Dark, Light).
- Build error handling and notification banner.
- Author foundational unit tests and documentation.

### 0.2.0 â€” Local Wallpapers
- Implement `IWallpaperService` with Windows `IDesktopWallpaper` COM API and `SystemParametersInfo` fallback.
- Support local image indexing and fast zero-allocation header decoding (PNG, JPG, WebP, BMP).
- Multi-monitor support: detect display topologies, resolutions, and assign wallpapers per monitor or across all displays.
- Implement capture of original desktop wallpaper state prior to changes for safe 1-click restoration/undo.
- Implement responsive thumbnail caching without UI blocking using Avalonia `Bitmap.DecodeToWidth`.
- Implement user-controlled folder scanning, multi-image picking, favorites tracking, and local JSON preferences persistence.
- Author comprehensive unit tests covering ViewModel, header decoders, and persistence.

### 0.3.0 â€” Curated Downloadable Gallery
- Curated asset catalog with hybrid multi-source integration (Wallhaven 4K desktop library, Nekos Art archive, and offline JSON fallback).
- Strict download safety: HTTPS enforcement, allowed image formats, safety size quotas (50 MB cap).
- Download service with streaming progress indication, retry logic, cancellation, and partial file cleanup.
- SHA-256 hash integrity checking for downloaded assets with corruption rejection.
- Offline-first storage management: downloaded assets are cached locally in app data and remain accessible without internet.
- Rich UI with search debouncing, category filtering (Nature, Space, Abstract, Architecture, Minimal, Anime), asset detail inspector, and 1-click wallpaper application.
- Comprehensive unit test coverage across catalog parsing, download pipeline, multi-source orchestration, and ViewModel lifecycle (57 automated tests passing).

### 0.4.0 â€” Icon Customization
- Implement `IIconService` using `IShellLinkW` / `IPersistFile` COM interfaces for non-destructive `.lnk` shortcut icon modification.
- Implement desktop folder icon customization via standard hidden `desktop.ini` and `FileAttributes.ReadOnly` directory flag.
- Safe, strictly non-destructive implementation: never modifies target application binaries (`.exe`), PE structures, or protected system files.
- Zero-allocation header validator (`IconValidator`) verifying format magic bytes, dimensions, offsets, and icon counts for `.ico` files and validating library container files (`.exe`, `.dll`).
- Automatic baseline snapshot capture prior to modification, persisted atomically to `%LocalAppData%\Deskora\icon_preferences.json`.
- 1-click individual rollback ("Restore Original Icon") and global rollback ("Reset All to Defaults").
- Clean Explorer cache notification via `SHChangeNotify(SHCNE_ASSOCCHANGED, SHCNF_FLUSH)` without terminating `explorer.exe`.
- Rich Avalonia UI with live desktop search, filtering, custom icon browsing dialog, badge indicators, and safety guarantee summary.
- Comprehensive unit test coverage across validator, persistence, and ViewModel orchestration (78 automated tests passing).

### 0.5.0 â€” Desktop Layouts and Presets
- Query `SysListView32` desktop control via `LVM_GETITEMPOSITION`.
- Save coordinates in structured, human-readable JSON files.
- Restore coordinates via `LVM_SETITEMPOSITION` across resolution changes.
- Handle newly added or removed shortcuts gracefully without overlap.

### 0.6.0 â€” Live Wallpaper Proof of Concept
- Research and test Windows `Progman` / `WorkerW` child window parenting.
- Implement lightweight video loop player in an isolated child process.
- Monitor foreground window state to suspend video rendering during gaming or full-screen work.
- Graceful detach and desktop restoration on exit.

### 0.7.0 â€” In-App and Floating Widgets
- Develop modular widget components (clock, system stats, quick notes).
- Enforce strict resource budgets (<30MB RAM, <0.1% idle CPU).
- Support dual docking: in-app sidebar widget vs. floating desktop widget.
- Window layering flags (`WS_EX_TOOLWINDOW`, `WS_EX_NOACTIVATE`).

### 0.8.0 â€” Themes and Integration
- Extract dominant accent colors from active wallpapers.
- Align Windows DWM system accent colors with Deskora themes.
- Support importing and exporting comprehensive theme preset bundles.

### 0.9.0 â€” Beta Hardening and Installer
- Memory leak analysis, crash recovery validation, and accessibility audit.
- Windows packaging (MSIX / standalone installer).
- Documentation polishing and clean uninstall verification.

### 1.0.0 â€” Stable Release
- Stable release readiness.
- Product Owner approval on final release notes and license.

