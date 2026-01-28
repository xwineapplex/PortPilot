# PortPilot

English | [繁體中文](README_CHT.md)

## Overview

PortPilot is a cross-platform display input switcher built with .NET 8 and Avalonia UI.
It watches USB device connect/disconnect events and switches monitor input sources
via DDC/CI (VCP 0x60).

Typical use case: pair it with a USB switch or KVM so your monitor input follows
the active computer automatically.

## Features

- Monitor USB device add/remove events (by VID/PID)
- Two-way triggers:
  - Switch to input A when the device is connected (Added)
  - Switch to input B when the device is disconnected (Removed)
- Detect monitors that support DDC/CI
- Common input source presets
- USB target filtering (safe list) for specific VID/PID devices
- Localization (I18N): System default / English / Traditional Chinese (restart required)
- System tray resident mode:
  - Closing the window (X) minimizes to the tray by default
  - Tray menu can quickly toggle Monitoring Active / Inactive
- Debug mode:
  - Records raw USB events and debug logs only when enabled
  - Debug logs can be saved to a file (debug-log.txt)

## Supported Platforms & Requirements

### Windows
- Uses dxva2.dll for DDC/CI communication
- Uses WMI to watch USB events
- Ensure your monitor supports DDC/CI and enable it in the monitor OSD

### Linux
- Uses ddcutil for DDC/CI communication
- Uses udevadm to watch USB events
- Requirement: install ddcutil and configure permissions (see Linux setup)

#### Linux System Tray Compatibility
- KDE Plasma: native support (StatusNotifierItem)
- GNOME: typically requires AppIndicator/KStatusNotifierItem shell extensions
- Wayland: depends on the desktop environment and DBus support

> The app uses a single cross-platform tray implementation. In some environments
> the tray icon may appear as an empty placeholder, but the right-click menu and
> functionality still work.

## System Tray

- Tooltip: "PortPilot is running" (localized)

### Left Click
- If the main window is hidden: restore and show it
- If already shown: bring it to the front (activate)

### Right-Click Menu
- Open PortPilot
- Monitoring Active / Monitoring Inactive
  - Strictly synced with the "Enable monitoring" toggle in the main window
- Exit
  - Saves settings, then fully quits (not affected by "minimize to tray on close")

## Linux Setup (Non-root)

On Linux, controlling monitors via DDC/CI requires read/write access to /dev/i2c-*.
These devices are root-only by default. Follow the steps below to enable access
for your user account.

### 1. Install required packages

Fedora / RHEL:

```bash
sudo dnf install ddcutil i2c-tools
```

Debian / Ubuntu:

```bash
sudo apt install ddcutil i2c-tools
```

### 2. Load the I2C kernel module

Load immediately:

```bash
sudo modprobe i2c-dev
```

Enable at boot:

```bash
echo "i2c-dev" | sudo tee /etc/modules-load.d/i2c.conf
```

### 3. Create the i2c group and add your user

```bash
sudo groupadd --system i2c
sudo usermod -aG i2c $USER
```

### 4. Add udev rules (required)

Create the rule file:

```bash
sudo nano /etc/udev/rules.d/45-ddcutil-i2c.rules
```

Add the content below:

```
KERNEL=="i2c-[0-9]*", GROUP="i2c", MODE="0660"
```

Reload rules:

```bash
sudo udevadm control --reload-rules
sudo udevadm trigger
```

### 5. Apply changes and verify

Important: log out and log in again (or reboot) for group changes to apply.

Verify:

1. Run `groups` and confirm `i2c` is listed.
2. Run `ddcutil detect` without sudo. If you see monitor info with no permission
   errors, the setup is complete.

## Usage

1. Launch the app and pick a monitor from the Monitor dropdown
2. Set the input to switch to when the device is CONNECTED (Added)
3. Set the input to switch to when the device is DISCONNECTED (Removed)
4. Choose USB targets (safe list) to watch, such as a USB switch or hub
5. Click Add/Update rule
6. Click Save (or rely on auto-save after Add/Update rule)

## Configuration File

The configuration is stored as JSON and saved automatically.

```jsonc
{
  "language": "auto",
  "rules": [
    {
      "vid": "0BDA",
      "pid": "0411",
      "onAdded": {
        "monitorId": "10001:0", // Windows format
        "inputSource": 15
      },
      "onRemoved": {
        "monitorId": "10001:0",
        "inputSource": 16
      }
    }
  ],
  "lastSelectedMonitorId": "10001:0",
  "lastInputSource": 15,
  "minimizeToTrayOnClose": true,
  "monitoringEnabled": true
}
```

Notes:
- Windows monitorId format: "10001:0" (HMONITOR:Index)
- Linux monitorId format: "1" (I2C bus number)

### Settings

- language:
  - "auto": follow system language
  - "en-US": English
  - "zh-Hant": Traditional Chinese
  - Restart required to apply language changes
- minimizeToTrayOnClose:
  - true: closing the window (X) minimizes to tray
  - false: closing the window exits the app
- monitoringEnabled:
  - controls whether the USB monitoring service is active

## Input Source Codes (VCP 0x60)

| Code (Hex) | Meaning |
| :-- | :-- |
| 0x0F | DisplayPort 1 |
| 0x10 | DisplayPort 2 |
| 0x11 | HDMI 1 |
| 0x12 | HDMI 2 |
| 0x01 | D-Sub (VGA) |

## Localization (I18N)

1. Open Settings (bottom-right button in the main window)
2. Choose Language: System Default / English / Traditional Chinese
3. Save and restart when prompted

## License

See [LICENSE.txt](LICENSE.txt).

## Contributing / Developer Guide

See [docs/DEVELOPER_GUIDE.md](docs/DEVELOPER_GUIDE.md).

## Norms & Policies

- [.github/copilot-instructions.md](.github/copilot-instructions.md)
- [docs/COMMENT_STYLE_GUIDE.md](docs/COMMENT_STYLE_GUIDE.md)
- [docs/NAMING_CONVENTION.md](docs/NAMING_CONVENTION.md)
