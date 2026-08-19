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
│   └── PortPilot.ico
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
├── Linux/
│   ├── LinuxMonitorController.cs
│   ├── LinuxUsbEventParser.cs
│   ├── LinuxUsbWatcher.cs
│   └── UdevMonitorProcess.cs
├── docs/
│   ├── COMMENT_STYLE_GUIDE.md
│   ├── HIGH_PRIORITY_REMEDIATION_PLAN.md
│   ├── NAMING_CONVENTION.md
│   └── RELEASE_PROCESS.md
└── tests/
    └── PortPilot.Tests/
        ├── Fixtures/
        ├── Support/
        ├── TestDoubles/
        └── PortPilot.Tests.csproj
```

Key areas:

- Abstractions: shared interfaces for monitors, USB watchers, and tray control
- Config: config models and persistence
- Properties: resource files and generated wrappers for localization
- ViewModels / Views: UI logic and Avalonia XAML views
- Windows / Linux: platform-specific USB watcher and monitor control
- tests/PortPilot.Tests: headless regression tests, fixtures, and test doubles

## Platform Implementations

### Windows

- WinUsbWatcher: watches USB events through WMI
- WinMonitorController: controls monitor input via dxva2.dll (DDC/CI)

### Linux

- LinuxUsbEventParser: parses complete udevadm event blocks at blank lines, headers, and EOF
- LinuxUsbWatcher: owns the udevadm process and delivers filtered USB device events
- UdevMonitorProcess: adapts process startup, stream draining, termination, and cleanup
- LinuxMonitorController: controls monitor input via ddcutil (DDC/CI)

### Shared Logic

- MainWindowViewModel: orchestrates UI state, rule management, and service state
- App: owns and disposes the main view model before desktop lifetime teardown

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

Key naming rules: see [NAMING_CONVENTION.md](NAMING_CONVENTION.md).

## Build & Run

Prerequisite: .NET 10 SDK.

Primary packages:
- Avalonia 11.3.20 (DataGrid 11.3.13)
- CommunityToolkit.Mvvm 8.4.2
- System.Management 10.0.11

Typical commands:

```bash
dotnet restore
dotnet build --no-restore
dotnet test --no-restore
dotnet run --no-build
```

## Automated Tests

The `tests/PortPilot.Tests` project uses xUnit and runs without an Avalonia display
server, physical USB devices, monitors, WMI, `udevadm`, or `ddcutil`.

The regression suite currently covers:

- udevadm event parsing at blank lines, new headers, and EOF
- malformed and incomplete event handling
- Linux watcher start, stop, restart, process failure, and device filtering
- application shutdown racing with asynchronous initialization
- temporary configuration persistence used by later remediation batches

Use fixtures under `tests/PortPilot.Tests/Fixtures` for representative process output.
Use test doubles under `tests/PortPilot.Tests/TestDoubles` instead of accessing platform
services or hardware.

## Debugging & Logging

- Enable Debug mode in the app to record raw USB events and debug logs.
- Debug logs can be saved to debug-log.txt.

## Release / Publish

GitHub Actions builds all supported release variants when a `v*` tag is pushed.
Each tag requires a matching Markdown file under `.github/release-notes/`, and the
workflow creates a draft GitHub Release for review.

Follow [RELEASE_PROCESS.md](RELEASE_PROCESS.md) to update the version, prepare
AI-assisted release notes, validate the build, and publish a release safely.

## Maintenance Plans

- Follow [HIGH_PRIORITY_REMEDIATION_PLAN.md](HIGH_PRIORITY_REMEDIATION_PLAN.md) for the staged
  reliability, lifecycle, configuration, and monitor-identity remediation work.

## Norms & Policies

- [Copilot instructions](../.github/copilot-instructions.md)
- [Comment style guide](COMMENT_STYLE_GUIDE.md)
- [I18N naming conventions](NAMING_CONVENTION.md)
