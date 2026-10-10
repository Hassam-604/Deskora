# Deskora — Product Roadmap

This document outlines the sequential release milestones for **Deskora**. Development is strictly incremental: features are designed, tested, verified, and approved by the Product Owner before advancing to subsequent milestones.

---

## Milestone Overview

| Version | Milestone Objective | Status | Focus |
|---|---|---|---|
| **0.1.0** | **Foundation and App Shell** | **Completed** | Solution architecture, navigation shell, design system, theme engine, tests |
| **0.2.0** | **Local Wallpapers** | Planned | Local folder browsing, multi-monitor configuration, aspect ratio fit, undo/restore |
| **0.3.0** | **Curated Downloadable Gallery** | Planned | Approved asset catalog, previews, licensing metadata, download manager, offline storage |
| **0.4.0** | **Icon Customization** | Planned | Desktop shortcut icons, Windows shell cache refresh, backup points, safe reset |
| **0.5.0** | **Desktop Layouts and Presets** | Planned | SysListView32 icon coordinate capture, multi-monitor resolution restoration, named profiles |
| **0.6.0** | **Live Wallpaper Proof of Concept** | Planned | WorkerW canvas hooking, video playback engine, auto-pause on fullscreen, crash isolation |
| **0.7.0** | **In-App and Floating Widgets** | Planned | Memory-budgeted desktop widgets (clocks, stats), WS_EX_TOOLWINDOW layering |
| **0.8.0** | **Themes and Integration** | Planned | DWM accent color matching, coordinated wallpaper/widget/icon bundles |
| **0.9.0** | **Beta Hardening and Installer** | Planned | End-to-end stress testing, crash telemetry reporting, MSIX/InnoSetup installer |
| **1.0.0** | **Stable Release** | Planned | Full production documentation, finalized licensing, release packaging |

---

## Milestone Details

### 0.1.0 — Foundation and App Shell (Current)
- Establish solution structure (`Deskora.Core`, `Deskora.App`, `Deskora.Tests`).
- Implement navigation shell with responsive resizing.
- Create 9 modules (Home, Wallpapers, Live Wallpapers, Icons, Layouts, Themes, Widgets, Settings, About).
- Implement active theme engine (System Default, Dark, Light).
- Build error handling and notification banner.
- Author foundational unit tests and documentation.

### 0.2.0 — Local Wallpapers
- Implement `IWallpaperService` with Windows `IDesktopWallpaper` COM API.
- Support local image indexing (PNG, JPG, WebP, BMP).
- Multi-monitor support: detect display topologies and assign wallpapers per monitor.
- Implement capture of original wallpaper state for guaranteed 1-click restoration.
- Implement responsive thumbnail caching without UI blocking.

### 0.3.0 — Curated Downloadable Gallery
- Curated asset catalog using approved, verified open licenses.
- Download service with progress indication, retry logic, and cancellation.
- Hash integrity checking for downloaded assets.
- Offline-first storage management with size quotas.

### 0.4.0 — Icon Customization
- Implement `IIconService` using `IShellLinkW` for `.lnk` shortcuts.
- Support `.ico` asset extraction and validation.
- Clean Explorer cache notification via `SHChangeNotify(SHCNE_ASSOCCHANGED)`.
- Full rollback mechanism to restore default desktop icons.

### 0.5.0 — Desktop Layouts and Presets
- Query `SysListView32` desktop control via `LVM_GETITEMPOSITION`.
- Save coordinates in structured, human-readable JSON files.
- Restore coordinates via `LVM_SETITEMPOSITION` across resolution changes.
- Handle newly added or removed shortcuts gracefully without overlap.

### 0.6.0 — Live Wallpaper Proof of Concept
- Research and test Windows `Progman` / `WorkerW` child window parenting.
- Implement lightweight video loop player in an isolated child process.
- Monitor foreground window state to suspend video rendering during gaming or full-screen work.
- Graceful detach and desktop restoration on exit.

### 0.7.0 — In-App and Floating Widgets
- Develop modular widget components (clock, system stats, quick notes).
- Enforce strict resource budgets (<30MB RAM, <0.1% idle CPU).
- Support dual docking: in-app sidebar widget vs. floating desktop widget.
- Window layering flags (`WS_EX_TOOLWINDOW`, `WS_EX_NOACTIVATE`).

### 0.8.0 — Themes and Integration
- Extract dominant accent colors from active wallpapers.
- Align Windows DWM system accent colors with Deskora themes.
- Support importing and exporting comprehensive theme preset bundles.

### 0.9.0 — Beta Hardening and Installer
- Memory leak analysis, crash recovery validation, and accessibility audit.
- Windows packaging (MSIX / standalone installer).
- Documentation polishing and clean uninstall verification.

### 1.0.0 — Stable Release
- Stable release readiness.
- Product Owner approval on final release notes and license.
