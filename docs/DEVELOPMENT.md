# Deskora — Development Guide

Welcome to the **Deskora** development documentation. This guide explains how to set up your environment, understand the solution architecture, run automated tests, and contribute safely.

---

## 1. Prerequisites

- **Operating System:** Windows 10/11 (Version 19045 or later recommended).
- **.NET SDK:** .NET 8.0 SDK (8.0.400 or later).
- **Git:** Git for Windows (v2.40+).
- **IDE:** Visual Studio 2022, JetBrains Rider, or Visual Studio Code (with C# Dev Kit and Avalonia extension).

---

## 2. Solution Architecture

Deskora strictly isolates user interface presentation from core domain logic and platform-specific desktop services:

```
┌──────────────────────────────────────────────┐
│                  Deskora.App                 │
│         (Avalonia UI Presentation Layer)     │
│   • Views (XAML + Code-Behind)               │
│   • Design System & Styles                   │
│   • Converters & Resources                   │
└──────────────────────┬───────────────────────┘
                       │ References
┌──────────────────────▼───────────────────────┐
│                 Deskora.Core                 │
│       (Platform-Agnostic Domain Logic)       │
│   • ViewModels (CommunityToolkit.Mvvm)       │
│   • Models & Enums                           │
│   • Service Contracts (INavigationService...) │
└──────────────────────▲───────────────────────┘
                       │ Tested by
┌──────────────────────┴───────────────────────┐
│                Deskora.Tests                 │
│             (xUnit Automated Tests)          │
└──────────────────────────────────────────────┘
```

### Key Architectural Rules:
1. **Never put Win32 API calls directly in Views or ViewModels.** Future platform-specific code belongs behind clean interfaces in `Deskora.Core.Services` or dedicated platform libraries.
2. **Use MVVM with CommunityToolkit.Mvvm.** ViewModels inherit from `ViewModelBase` and use `[ObservableProperty]` and `[RelayCommand]`.
3. **No fake controls.** Never add a button or toggle that appears functional but performs no action. If a feature is planned for a future milestone, explicitly document it as a roadmap item or display a planned badge.
4. **Theme and styling.** UI elements must use dynamic resource tokens (`DynamicResource AppBackgroundBrush`, etc.) to support seamless live theme switching.

---

## 3. Common Development Workflows

### Building the Solution
```powershell
dotnet build
```

### Running Automated Tests
```powershell
dotnet test
```

### Running Deskora Locally
```powershell
dotnet run --project src/Deskora.App
```

---

## 4. How to Add a New Page Module

1. **Define the Navigation Key:**
   Add a new entry to `Deskora.Core.Models.NavigationPageKey`.

2. **Create the ViewModel:**
   Create `src/Deskora.Core/ViewModels/MyNewViewModel.cs` inheriting from `ViewModelBase`.

3. **Create the View:**
   Create `src/Deskora.App/Views/MyNewView.axaml` and `MyNewView.axaml.cs`.
   The `ViewLocator` will automatically associate `MyNewViewModel` with `MyNewView` based on the naming convention.

4. **Register the Page:**
   In `src/Deskora.App/App.axaml.cs`, add:
   ```csharp
   navigationService.RegisterPage(NavigationPageKey.MyNewPage, () => new MyNewViewModel());
   ```

5. **Write Unit Tests:**
   Add unit tests in `tests/Deskora.Tests` verifying navigation and ViewModel behavior.

---

## 5. Security & Safety Guidelines

- **Never commit secrets:** No API keys, credentials, machine-specific paths, or tokens.
- **Inspect `.gitignore`:** Ensure temporary files, logs, and build artifacts (`bin/`, `obj/`) remain untracked.
- **Rollback first:** Before adding features that alter Windows system state (wallpapers, shortcuts, registry), always implement and test the snapshot and restore mechanism first.
- **Controlled commits:** Always review `git status` and `git diff` manually before staging or committing changes.
