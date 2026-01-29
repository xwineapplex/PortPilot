# Projects and dependencies analysis

This document provides a comprehensive overview of the projects and their dependencies in the context of upgrading to .NETCoreApp,Version=v0.0.

## Table of Contents

- [Executive Summary](#executive-Summary)
  - [Highlevel Metrics](#highlevel-metrics)
  - [Projects Compatibility](#projects-compatibility)
  - [Package Compatibility](#package-compatibility)
  - [API Compatibility](#api-compatibility)
- [Aggregate NuGet packages details](#aggregate-nuget-packages-details)
- [Top API Migration Challenges](#top-api-migration-challenges)
  - [Technologies and Features](#technologies-and-features)
  - [Most Frequent API Issues](#most-frequent-api-issues)
- [Projects Relationship Graph](#projects-relationship-graph)
- [Project Details](#project-details)

  - [PortPilot-Project.csproj](#portpilot-projectcsproj)


## Executive Summary

### Highlevel Metrics

| Metric | Count | Status |
| :--- | :---: | :--- |
| Total Projects | 1 | 0 require upgrade |
| Total NuGet Packages | 8 | All compatible |
| Total Code Files | 23 |  |
| Total Code Files with Incidents | 0 |  |
| Total Lines of Code | 2432 |  |
| Total Number of Issues | 0 |  |
| Estimated LOC to modify | 0+ | at least 0.0% of codebase |

### Projects Compatibility

| Project | Target Framework | Difficulty | Package Issues | API Issues | Est. LOC Impact | Description |
| :--- | :---: | :---: | :---: | :---: | :---: | :--- |
| [PortPilot-Project.csproj](#portpilot-projectcsproj) | net8.0 | ✅ None | 0 | 0 |  | WinForms, Sdk Style = True |

### Package Compatibility

| Status | Count | Percentage |
| :--- | :---: | :---: |
| ✅ Compatible | 8 | 100.0% |
| ⚠️ Incompatible | 0 | 0.0% |
| 🔄 Upgrade Recommended | 0 | 0.0% |
| ***Total NuGet Packages*** | ***8*** | ***100%*** |

### API Compatibility

| Category | Count | Impact |
| :--- | :---: | :--- |
| 🔴 Binary Incompatible | 0 | High - Require code changes |
| 🟡 Source Incompatible | 0 | Medium - Needs re-compilation and potential conflicting API error fixing |
| 🔵 Behavioral change | 0 | Low - Behavioral changes that may require testing at runtime |
| ✅ Compatible | 0 |  |
| ***Total APIs Analyzed*** | ***0*** |  |

## Aggregate NuGet packages details

| Package | Current Version | Suggested Version | Projects | Description |
| :--- | :---: | :---: | :--- | :--- |
| Avalonia | 11.3.9 |  | [PortPilot-Project.csproj](#portpilot-projectcsproj) | ✅Compatible |
| Avalonia.Controls.DataGrid | 11.3.9 |  | [PortPilot-Project.csproj](#portpilot-projectcsproj) | ✅Compatible |
| Avalonia.Desktop | 11.3.9 |  | [PortPilot-Project.csproj](#portpilot-projectcsproj) | ✅Compatible |
| Avalonia.Diagnostics | 11.3.9 |  | [PortPilot-Project.csproj](#portpilot-projectcsproj) | ✅Compatible |
| Avalonia.Fonts.Inter | 11.3.9 |  | [PortPilot-Project.csproj](#portpilot-projectcsproj) | ✅Compatible |
| Avalonia.Themes.Fluent | 11.3.9 |  | [PortPilot-Project.csproj](#portpilot-projectcsproj) | ✅Compatible |
| CommunityToolkit.Mvvm | 8.2.1 |  | [PortPilot-Project.csproj](#portpilot-projectcsproj) | ✅Compatible |
| System.Management | 8.0.0 |  | [PortPilot-Project.csproj](#portpilot-projectcsproj) | ✅Compatible |

## Top API Migration Challenges

### Technologies and Features

| Technology | Issues | Percentage | Migration Path |
| :--- | :---: | :---: | :--- |

### Most Frequent API Issues

| API | Count | Percentage | Category |
| :--- | :---: | :---: | :--- |

## Projects Relationship Graph

Legend:
📦 SDK-style project
⚙️ Classic project

```mermaid
flowchart LR
    P1["<b>📦&nbsp;PortPilot-Project.csproj</b><br/><small>net8.0</small>"]
    click P1 "#portpilot-projectcsproj"

```

## Project Details

<a id="portpilot-projectcsproj"></a>
### PortPilot-Project.csproj

#### Project Info

- **Current Target Framework:** net8.0✅
- **SDK-style**: True
- **Project Kind:** WinForms
- **Dependencies**: 0
- **Dependants**: 0
- **Number of Files**: 25
- **Lines of Code**: 2432
- **Estimated LOC to modify**: 0+ (at least 0.0% of the project)

#### Dependency Graph

Legend:
📦 SDK-style project
⚙️ Classic project

```mermaid
flowchart TB
    subgraph current["PortPilot-Project.csproj"]
        MAIN["<b>📦&nbsp;PortPilot-Project.csproj</b><br/><small>net8.0</small>"]
        click MAIN "#portpilot-projectcsproj"
    end

```

### API Compatibility

| Category | Count | Impact |
| :--- | :---: | :--- |
| 🔴 Binary Incompatible | 0 | High - Require code changes |
| 🟡 Source Incompatible | 0 | Medium - Needs re-compilation and potential conflicting API error fixing |
| 🔵 Behavioral change | 0 | Low - Behavioral changes that may require testing at runtime |
| ✅ Compatible | 0 |  |
| ***Total APIs Analyzed*** | ***0*** |  |

