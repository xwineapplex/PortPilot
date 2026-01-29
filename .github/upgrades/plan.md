# .NET 10 升級計畫（All-At-Once Strategy）

## Table of Contents
- [Executive Summary](#executive-summary)
- [Migration Strategy](#migration-strategy)
- [Detailed Dependency Analysis](#detailed-dependency-analysis)
- [Project-by-Project Plans](#project-by-project-plans)
- [Package Update Reference](#package-update-reference)
- [Breaking Changes Catalog](#breaking-changes-catalog)
- [Testing & Validation Strategy](#testing--validation-strategy)
- [Risk Management](#risk-management)
- [Complexity & Effort Assessment](#complexity--effort-assessment)
- [Source Control Strategy](#source-control-strategy)
- [Success Criteria](#success-criteria)

## Executive Summary
**Scenario**: 將現有 `net8.0` WinForms 專案升級至 `net10.0`（.NET 10 LTS）。

### Selected Strategy
**All-At-Once Strategy** - 單一專案、無相依關係，適合一次性升級。

**Rationale**:
- 1 個專案（小型解決方案）
- 相依深度 0
- 無安全性弱點與套件升級建議
- 變更範圍集中、可一次完成

### Solution Metrics (from assessment)
- Projects: 1
- Total NuGet Packages: 8（皆相容）
- LOC: 2432
- API Issues: 0
- Security Vulnerabilities: 0

### Complexity Classification
**Simple**：?5 專案、無相依、無高風險或弱點。

### Iteration Strategy Used
Simple solution → 1 次整體詳細填寫 + 最終校準。

## Migration Strategy
**Approach**: All-At-Once Strategy（單一協調升級、無中間狀態）。

**Justification**:
- 解決方案僅 1 個專案
- 無專案相依與循環依賴
- 套件皆相容且無安全性弱點

**Execution Model**:
- 單次協調更新 `TargetFramework` 至 `net10.0`
- 若後續需要更新套件，與框架升級同一批次進行
- 統一進行建置與測試驗證

## Detailed Dependency Analysis
**Dependency Graph Summary**:
- `PortPilot-Project.csproj` 為唯一專案
- 無專案相依、無循環依賴

**Migration Ordering**:
- 無需分階段或葉節點排序
- 全案同時升級（原子性操作）

## Project-by-Project Plans

### Project: `PortPilot-Project.csproj`
**Current State**:
- Target Framework: `net8.0`
- Project Kind: WinForms
- SDK-style: True
- Dependencies: 0
- Dependants: 0
- Packages: 8（皆相容）
- LOC: 2432
- Risk Level: Low

**Target State**:
- Target Framework: `net10.0`
- Packages: 無升級建議（維持相容版本）

**Migration Steps**:
1. **Project File Update**: 將 `TargetFramework` 更新為 `net10.0`。
2. **Package References**: 評估所有套件在 `net10.0` 下的相容性（評估顯示皆相容）。
3. **Breaking Changes Review**: 針對 .NET 10 與 WinForms 可能變更做編譯與測試確認。
4. **Build & Fix**: 以升級後框架建置，修正任何編譯錯誤（若出現）。
5. **Test**: 執行測試與基本功能驗證（若有測試專案或手動驗證流程）。

**Expected Breaking Changes**:
- 依實際編譯/測試結果確認（見「Breaking Changes Catalog」）。

**Testing Strategy**:
- 建置成功且無錯誤
- 若有測試：單元測試全部通過
- 針對 UI 啟動流程進行最小驗證（若有自動化或既有流程）

**Validation Checklist**:
- [ ] 專案能以 `net10.0` 建置成功
- [ ] 無警告/錯誤（或已評估保留）
- [ ] 測試全部通過（若適用）

## Package Update Reference
**Assessment 結果**：無套件升級建議，全部顯示相容。

| Package | Current Version | Target Version | Projects Affected | Reason |
|---|---:|---:|---|---|
| Avalonia | 11.3.9 | (no change) | `PortPilot-Project.csproj` | ? Compatible |
| Avalonia.Controls.DataGrid | 11.3.9 | (no change) | `PortPilot-Project.csproj` | ? Compatible |
| Avalonia.Desktop | 11.3.9 | (no change) | `PortPilot-Project.csproj` | ? Compatible |
| Avalonia.Diagnostics | 11.3.9 | (no change) | `PortPilot-Project.csproj` | ? Compatible |
| Avalonia.Fonts.Inter | 11.3.9 | (no change) | `PortPilot-Project.csproj` | ? Compatible |
| Avalonia.Themes.Fluent | 11.3.9 | (no change) | `PortPilot-Project.csproj` | ? Compatible |
| CommunityToolkit.Mvvm | 8.2.1 | (no change) | `PortPilot-Project.csproj` | ? Compatible |
| System.Management | 8.0.0 | (no change) | `PortPilot-Project.csproj` | ? Compatible |

## Breaking Changes Catalog
> ?? 需於建置與測試階段確認是否有實際變更衝擊。

可能關注區域：
- .NET 10 針對 WinForms 行為變更
- 行為/預設值調整導致 UI 或系統 API 變更
- API 變更或棄用（如有編譯錯誤會具體呈現）

## Testing & Validation Strategy
**Scope**: 單一專案、單次整體驗證

- **Build Validation**:
  - `net10.0` 建置成功
  - 無編譯錯誤
- **Test Validation**:
  - 若存在測試：全部通過
- **Runtime Validation**:
  - 如有自動化 UI/啟動流程，執行最小驗證

## Risk Management
| Risk | Level | Description | Mitigation |
|---|---|---|---|
| Framework upgrade regression | Low | .NET 10 行為差異可能影響 UI 或 API | 以建置/測試回饋為準，必要時調整呼叫或設定 |
| Hidden runtime issues | Low | 編譯未揭示之執行期差異 | 最小可行 UI 啟動/基本流程驗證 |

## Complexity & Effort Assessment
| Project | Complexity | Dependencies | Notes |
|---|---|---|---|
| `PortPilot-Project.csproj` | Low | 0 | 單一 WinForms 專案，套件相容 |

## Source Control Strategy
- 既定升級分支：`upgrade-to-NET10`
- **All-at-once** 原則：單次提交涵蓋框架與相關修正
- 建議單一提交或單一 PR（包含：框架升級 + 修正 + 測試結果）

## Success Criteria
- 全專案升級至 `net10.0`
- 所有套件維持相容（或依評估更新）
- 建置無錯誤
- 測試全部通過（若適用）
- 無安全性弱點遺留
