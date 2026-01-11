# PortPilot

PortPilot 是一個以 `.NET 8` + `Avalonia UI` 實作的跨平台螢幕訊號切換工具。
透過監聽 USB 裝置連線/斷線事件作為觸發，透過 DDC/CI 協定自動切換螢幕的輸入訊號源 (VCP `0x60`)。

典型應用場景：搭配 USB Switch / KVM，在切換不同電腦時自動讓螢幕訊號跟著切換到對應的輸入源。

## 功能

- 監聽 USB 裝置插入/移除事件 (依據 VID/PID 判斷)
- 雙向觸發動作：
  - 裝置 **Connected (Added)** 時切換至輸入源 A
  - 裝置 **Disconnected (Removed)** 時切換至輸入源 B
- 偵測支援 DDC/CI 的螢幕
- Input Source 提供常見預設值
- USB 目標過濾 (Safe list)：僅監聽特定 VID/PID 的裝置
- 系統匣 (System Tray) 常駐：
  - 關閉視窗（X）預設不會結束程式，會縮小到系統匣
  - 可透過系統匣右鍵選單快速調整「監控中 / 未監控」
- `Debug mode` 開關：
  - 開啟時才記錄 raw USB events 與 Debug log
  - Debug log 可儲存至檔案 (`debug-log.txt`)

## 平台支援 / 需求

### Windows
- 使用 `dxva2.dll` 進行 DDC/CI 通訊
- 使用 WMI 監聽 USB 事件
- 需確認螢幕支援 DDC/CI 並在螢幕 OSD 設定中開啟

### Linux
- 使用 `ddcutil` 進行 DDC/CI 通訊
- 使用 `udevadm` 監聽 USB 事件
- **系統需求**：需安裝 `ddcutil` 並設定權限 (詳見下方 Linux 設定指南)

#### Linux 系統匣相容性說明
- KDE Plasma：原生支援 (StatusNotifierItem)
- GNOME：通常需要安裝 AppIndicator / KStatusNotifierItem 類型的 Shell Extension 才能顯示系統匣圖示
- Wayland：依桌面環境與 DBus 支援狀況而定

> 維持跨平台單一路徑的系統匣實作；在部分環境下系統匣圖示可能會出現空白佔位，但右鍵選單與功能仍可正常運作。

## 系統匣 (System Tray)

- Tooltip：固定顯示 `PortPilot`

### 左鍵點擊
- 若主視窗為隱藏狀態：顯示主視窗並還原 (Normal state)
- 若主視窗已顯示：將視窗帶至最上層 (Activate)

### 右鍵選單
- `Open PortPilot`
- `Monitoring Active (監控中)` / `Monitoring Inactive (未監控)`
  - 與主視窗的「啟用監控服務」嚴格同步
- `Exit`
  - 會先儲存設定，然後完全結束程序（不受「關閉視窗縮到系統匣」影響）

## Linux 設定指南 (免 Sudo)

在 Linux 系統中，控制顯示器（透過 DDC/CI 協定）需要讀寫 `/dev/i2c-*` 裝置。為了安全性，這些裝置預設僅限 Root 存取。本指南將引導你完成環境設定，讓 PortPilot 軟體能在一般使用者權限下執行。

### 1. 安裝必要套件

在 Fedora / RHEL 系統上，請執行：

```bash
sudo dnf install ddcutil i2c-tools
```

在 Debian / Ubuntu 系統上：

```bash
sudo apt install ddcutil i2c-tools
```

### 2. 載入 I2C 核心模組

DDC/CI 通訊依賴 `i2c-dev` 核心模組。

立即載入：

```bash
sudo modprobe i2c-dev
```

設定開機自動載入：

```bash
echo "i2c-dev" | sudo tee /etc/modules-load.d/i2c.conf
```

### 3. 設定使用者群組與權限

Fedora 等發行版預設可能不會建立 `i2c` 群組，我們需要手動建立並將目前使用者加入。

建立群組並加入使用者：

```bash
# 建立系統群組 i2c
sudo groupadd --system i2c

# 將目前使用者 ($USER) 加入 i2c 群組
sudo usermod -aG i2c $USER
```

### 4. 設定 udev 規則 (關鍵步驟)

建立一個自定義規則，讓系統在發現 I2C 裝置時，自動將權限分配給 `i2c` 群組。

建立規則檔案：

```bash
sudo nano /etc/udev/rules.d/45-ddcutil-i2c.rules
```

在檔案中貼入以下內容：

```
# 讓 i2c 群組的使用者可以讀寫 i2c 裝置
KERNEL=="i2c-[0-9]*", GROUP="i2c", MODE="0660"
```

套用規則：

```bash
sudo udevadm control --reload-rules
sudo udevadm trigger
```

### 5. 套用變更與驗證

**重要：重新登入**

使用者群組的變更（usermod）需要登出並重新登入（或重啟電腦）後才會生效。

驗證步驟：

1. 檢查群組：輸入 `groups`，確認清單中包含 `i2c`。
2. 無 Sudo 測試：執行以下指令，若能看到螢幕資訊且無權限錯誤即成功。

```bash
ddcutil detect
```

## 使用方式

1. 啟動程式，在 `Monitor` 下拉選單選擇要控制的螢幕
2. 在 `When device CONNECTED (Added) switch to` 設定裝置連線時要切換的輸入源
3. 在 `When device DISCONNECTED (Removed) switch to` 設定裝置斷線時要切換的輸入源
4. 在 `USB targets (safe list)` 區塊，選擇要監聽的 USB 裝置 (例如 USB switch / hub)
5. 按 `Add/Update rule` 建立或更新規則
6. 按 `Save` (或由 `Add/Update rule` 自動儲存)

## 設定檔

設定檔為 JSON 格式，程式會自動儲存。

```json
{
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

> **注意**：
> - Windows `monitorId` 格式範例：`"10001:0"` (HMONITOR:Index)
> - Linux `monitorId` 格式範例：`"1"` (I2C Bus Number)

### 設定值說明

- `minimizeToTrayOnClose`:
  - `true`：按下視窗關閉鈕 (X) 時縮小到系統匣
  - `false`：按下視窗關閉鈕 (X) 時結束程式
- `monitoringEnabled`:
  - 控制 USB 監聽服務是否啟用（會被主視窗與系統匣選單同步更新）

## 輸入訊號代碼參考 (VCP Code 0x60)

| 代碼 (Hex) | 意義 |
| :-- | :-- |
| 0x0F | DisplayPort 1 |
| 0x10 | DisplayPort 2 |
| 0x11 | HDMI 1 |
| 0x12 | HDMI 2 |
| 0x01 | D-Sub (VGA) |

## 專案結構

```
PortPilot-Project/
├── PortPilot-Project.csproj
├── PortPilot-Project.slnx
├── README.md
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
├── Models/
│   └── InputSourceOption.cs
├── Tray/
│   └── AvaloniaTrayController.cs
├── ViewModels/
│   ├── MainWindowViewModel.cs
│   ├── RuleDisplayItem.cs
│   └── ViewModelBase.cs
├── Views/
│   ├── MainWindow.axaml
│   └── MainWindow.axaml.cs
├── Windows/
│   ├── WinMonitorController.cs
│   └── WinUsbWatcher.cs
└── Linux/
    ├── LinuxMonitorController.cs
    └── LinuxUsbWatcher.cs
```

## 實作細節

- **Windows**:
  - `WinUsbWatcher`: 使用 WMI 監聽 `Win32_PnPEntity` 事件。
  - `WinMonitorController`: 使用 Win32 API (`dxva2.dll`) 控制螢幕。
- **Linux**:
  - `LinuxUsbWatcher`: 使用 `udevadm info` 進行初始掃描，並搭配 `udevadm monitor` 監聽 USB 插拔事件，確保裝置路徑一致性。
  - `LinuxMonitorController`: 封裝 `ddcutil` 指令來偵測螢幕與設定 VCP。
- **共用邏輯**:
  - `MainWindowViewModel`: 負責 UI 邏輯、規則管理與跨平台介面的依賴注入。
