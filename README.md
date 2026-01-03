# PortPilot

PortPilot 是一個以 `.NET 8` + `Avalonia UI` 實作的 Windows 原型工具：
以「USB 裝置連線/斷線事件」作為觸發，透過 DDC/CI 將指定螢幕切換到指定的輸入來源（VCP `0x60`).

典型用途：搭配 USB Switch / KVM，在切到另一台電腦時自動把螢幕訊號切到另一個輸入。

## 功能

- 監聽 USB 裝置插入/拔除事件（以 VID/PID 配對）
- 規則支援雙向動作：
  - 裝置 **Connected (Added)** 時切換到輸入 A
  - 裝置 **Disconnected (Removed)** 時切換到輸入 B
- 支援選擇要控制的螢幕（DDC/CI）
- Input Source 以預設清單選擇（移除手動輸入）
- USB 目標清單採「safe list」：預設只顯示 `USB\...` 且具 VID/PID 的裝置，降低誤選 HID/SWD 裝置的機率
- `Debug mode` 開關：
  - 開啟時才顯示 raw USB events 與 Debug log
  - Debug log 可複製與另存檔（`debug-log.txt`，與 `config.json` 同資料夾）

## 平台 / 限制

- 目前主要針對 Windows（使用 `dxva2.dll` 進行 DDC/CI 控制、使用 WMI 監聽 USB 事件）
- 需要螢幕支援 DDC/CI 並在螢幕 OSD 設定中啟用（若未啟用，可能無法切換輸入源）

## 使用方式（基本流程）

1. 啟動程式後，先在 `Monitor` 下拉選單選擇要控制的螢幕
2. 在 `When device CONNECTED (Added) switch to` 選擇裝置連線時要切換的輸入
3. 在 `When device DISCONNECTED (Removed) switch to` 選擇裝置斷線時要切換的輸入
4. 在 `USB targets (safe list)` 中，選擇要監控的 USB 裝置（例如 USB switch / hub）
5. 按 `Add/Update rule` 建立或更新規則
6. 按 `Save`（或由 `Add/Update rule` 自動存）

## 設定檔

設定檔為 JSON，會由程式自動存取（路徑會顯示在 Status）。

目前規則格式（節錄）：

```json
{
  "rules": [
    {
      "vid": "0BDA",
      "pid": "0411",
      "onAdded": {
        "monitorId": "10001:0",
        "inputSource": 15
      },
      "onRemoved": {
        "monitorId": "10001:0",
        "inputSource": 16
      }
    }
  ],
  "lastSelectedMonitorId": "10001:0",
  "lastInputSource": 15
}
```

> `monitorId` 使用穩定格式 `"<HMONITOR_HEX>:<index>"`（例如 `"10001:0"`），避免使用 physical handle 導致 `Id="0"` 的問題。

## 輸入來源代碼參考（VCP Code 0x60）

| 代碼 (Hex) | 顯示 |
| :-- | :-- |
| 0x0F | DisplayPort 1 |
| 0x10 | DisplayPort 2 |
| 0x11 | HDMI 1 |
| 0x12 | HDMI 2 |
| 0x01 | D-Sub (VGA) |

## 專案結構（實際現況）

```
PortPilot-Project/
├── PortPilot-Project.csproj
├── README.md
├── Abstractions/
│   ├── IMonitorController.cs
│   ├── IUsbWatcher.cs
│   └── Models.cs
├── Config/
│   ├── AppConfig.cs
│   └── ConfigStore.cs
├── Models/
│   └── InputSourceOption.cs
├── ViewModels/
│   ├── MainWindowViewModel.cs
│   └── RuleDisplayItem.cs
├── Views/
│   ├── MainWindow.axaml
│   └── MainWindow.axaml.cs
└── Windows/
    ├── WinMonitorController.cs
    └── WinUsbWatcher.cs
```

## 實作概覽

- `Windows/WinUsbWatcher.cs`
  - 透過 WMI 監聽裝置建立/移除事件
  - 由 `DeviceID` 解析 VID/PID（例如 `USB\VID_046D&PID_0AB5\...`）
- `Windows/WinMonitorController.cs`
  - `EnumDisplayMonitors` + `GetPhysicalMonitorsFromHMONITOR` 取得可用螢幕
  - 以 VCP `0x60` 呼叫 `SetVCPFeature` 切換輸入源
- `ViewModels/MainWindowViewModel.cs`
  - 管理 UI 狀態、規則編輯、載入/儲存設定
  - USB 觸發時依 `Added/Removed` 執行對應動作

## 後續可能改進

- 對 USB 事件加入更細緻的過濾/去抖策略（依不同裝置的枚舉特性調整）
- 支援 Linux/macOS（目前僅設計上保留介面分層）
- 增加系統匣 / 自啟等整合功能
