# Developer Guide

## Overview

This guide focuses on project structure, platform-specific implementations, and
how to extend PortPilot safely.

## Architecture & Project Structure

```
PortPilot-Project/
├── PortPilot-Project.csproj
├── PortPilot-Project.slnx
├── README.md
├── README_CHT.md
├── LICENSE.txt
├── .gitignore
├── .gitattributes
├── app.manifest
├── Program.cs
├── App.axaml
├── App.axaml.cs
├── Assets/
│   └── avalonia-logo.ico
├── Abstractions/
│   ├── IMonitorController.cs
│   ├── IUsbWatcher.cs
│   ├── Models.cs
│   └── ITrayController.cs
├── Config/
│   ├── AppConfig.cs
│   └── ConfigStore.cs
├── Properties/
│   ├── Resources.cs
│   ├── Resources.resx
│   └── Resources.zh-Hant.resx
├── Models/
│   └── InputSourceOption.cs
├── Tray/
│   └── AvaloniaTrayController.cs
├── Utils/
│   └── AppRestart.cs
├── ViewModels/
│   ├── MainWindowViewModel.cs
│   ├── RuleDisplayItem.cs
│   ├── SettingsWindowViewModel.cs
│   └── ViewModelBase.cs
├── Views/
│   ├── MainWindow.axaml
│   ├── MainWindow.axaml.cs
│   ├── SettingsWindow.axaml
│   ├── SettingsWindow.axaml.cs
│   ├── MessageBoxWindow.axaml
│   └── MessageBoxWindow.axaml.cs
├── Windows/
│   ├── WinMonitorController.cs
│   └── WinUsbWatcher.cs
└── Linux/
   ├── LinuxMonitorController.cs
   └── LinuxUsbWatcher.cs
```

Key areas:
- Abstractions: shared interfaces for monitors, USB watchers, and tray control
- Config: config models and persistence
- Properties: resource files and generated wrappers for localization
- ViewModels / Views: UI logic and Avalonia XAML views
- Windows / Linux: platform-specific USB watcher and monitor control

## Platform Implementations

### Windows
- WinUsbWatcher: watches USB events through WMI
- WinMonitorController: controls monitor input via dxva2.dll (DDC/CI)

### Linux
- LinuxUsbWatcher: watches USB events through udevadm
- LinuxMonitorController: controls monitor input via ddcutil (DDC/CI)

### Shared Logic
- MainWindowViewModel: orchestrates UI state, rule management, and service state

## I18N Development

### Resource Files
- Properties/Resources.resx: default (English fallback)
- Properties/Resources.zh-Hant.resx: Traditional Chinese
- Properties/Resources.cs: resource wrapper used by XAML and C#

### Add a New Locale
1. Add a new resource file: Properties/Resources.<culture>.resx
2. Copy all keys from Properties/Resources.resx and translate them
3. Add the locale to the language list in SettingsWindowViewModel
4. Build and verify by switching language and restarting the app

### Add or Update a Resource Key
1. Add the key to Properties/Resources.resx (English)
2. Add the same key to Properties/Resources.zh-Hant.resx
3. Add a wrapper property in Properties/Resources.cs
4. Replace usage in code:
   - XAML: {x:Static p:Resources.<Key>}
   - C#: Resources.<Key> (use string.Format for parameters)

Key naming rules: see [docs/NAMING_CONVENTION.md](docs/NAMING_CONVENTION.md).

## Build & Run

Prerequisite: .NET 8 SDK.

Typical commands:
- dotnet restore
- dotnet build
- dotnet run

## Debugging & Logging

- Enable Debug mode in the app to record raw USB events and debug logs.
- Debug logs can be saved to debug-log.txt.

## Release / Publish

Typical command:
- dotnet publish -c Release

## Norms & Policies

- [.github/copilot-instructions.md](.github/copilot-instructions.md)
- [docs/COMMENT_STYLE_GUIDE.md](docs/COMMENT_STYLE_GUIDE.md)
- [docs/NAMING_CONVENTION.md](docs/NAMING_CONVENTION.md)
