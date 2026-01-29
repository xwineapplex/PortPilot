# .NET 10 升級任務清單（Execution）

> 分支：`upgrade-to-NET10`
> 目標：將 `PortPilot-Project` 從 `net8.0` 升級至 `net10.0`

## Dashboard
- 狀態：Not Started
- 主要輸出：升級後可成功 Restore/Build，且（若有）測試通過

## Tasks

### [▶] TASK-001: 驗證環境與基礎設定
- [▶] (1) 驗證已安裝可用的 .NET 10 SDK（或可建置 `net10.0`）
- [▶] (2) 檢查 `global.json`（若存在）是否允許使用 .NET 10 SDK
- [▶] (3) 確認目前在 `upgrade-to-NET10` 分支且工作目錄乾淨（無未提交變更）

### [ ] TASK-002: 更新專案目標框架至 `net10.0`
- [ ] (1) 更新 `PortPilot-Project.csproj`：將 `TargetFramework` 從 `net8.0` 改為 `net10.0`
- [ ] (2) Restore（NuGet/SDK restore）確認相依可還原
- [ ] (3) Build 專案確認無編譯錯誤

### [ ] TASK-003: 檢視並處理 NuGet 套件相容性（必要時）
- [ ] (1) 檢查 8 個套件在 `net10.0` 下 Restore/Build 是否仍相容
- [ ] (2) 若有套件不相容或警告升級需求：更新至支援 `net10.0` 的版本
- [ ] (3) 再次 Build 驗證

### [ ] TASK-004: 測試與最小執行期驗證
- [ ] (1) 探測是否存在測試專案（若無則略過單元測試）
- [ ] (2) 若存在測試：執行全部測試並確保 0 失敗
- [ ] (3) 進行最小可行執行期驗證（例如：啟動應用程式/核心流程 smoke test）

### [ ] TASK-005: 提交變更與完成收尾
- [ ] (1) 檢查變更摘要（僅包含升級相關變更）
- [ ] (2) Commit：`TASK-005: Upgrade project to net10.0`
- [ ] (3) 最終驗證：乾淨建置（Clean + Build）

## Completion Criteria
- `PortPilot-Project.csproj` 使用 `net10.0`
- Restore 成功
- Build 成功（0 errors）
- （若有）測試 0 failures
