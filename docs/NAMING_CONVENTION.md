# PortPilot I18N Naming Conventions (String Resource Keys)

Version: v1

## Purpose

This document defines naming rules for string resource keys used by PortPilot internationalization (I18N).

Goals:
- Predictable, discoverable keys
- Semantic meaning (avoid tying keys to a particular language)
- Low ambiguity for translators and reviewers

## Key Format

All keys SHOULD follow this structure:

```
[Context]_[Category]_[NameOrDescription]
```

- **Context**: Broad feature/module scope where the string appears.
- **Category** (optional): UI component or type (Btn, Label, Menu, Status, Tooltip, Title, etc.).
- **NameOrDescription**: Short semantic description of what the string is used for.

## Standard Prefixes (Context)

| Prefix | Scope / Use | Examples |
|---|---|---|
| `Common_` | Global/shared words (typically single words) | `Common_Save`, `Common_Cancel` |
| `Msg_` | Full sentences for messages/feedback (errors, confirmations, notifications) | `Msg_Error_ConfigLoadFailed`, `Msg_LanguageChangedRestart` |
| `{ViewName}_` | Strings used only within a specific window/view | `Settings_Title`, `Main_Btn_AddRule` |
| `Enum_` | Display names for enum/options shown to the user | `Enum_Lang_Auto`, `Enum_InputSource_HDMI1` |

## Do’s and Don’ts

### Do
- **Name by meaning**: `Main_Btn_AddRule` (clearly “Add rule button on Main window”).
- **Distinguish verb vs noun** when the same word can be ambiguous:
  - `Action_Open` (verb) vs `Status_Open` (adjective/state)
- **Keep keys stable** even when wording changes.

### Don’t
- **Don’t name by the literal text**: `String_Add` (bad: “Add” might mean different things elsewhere).
- **Don’t use control indices**: `Label1`, `Button3` (unmaintainable).

## Parameterized Strings

Use placeholders for variable content:
- Prefer .NET format placeholders: `{0}`, `{1}`, ...
- Example: `Msg_DeviceConnected` = `Device connected: {0}`

Rules:
- Keep placeholder ordering consistent across locales when possible.
- Avoid embedding markup or control characters in resource strings.

## Review Checklist

- Is the key semantic and stable?
- Does it have an appropriate prefix?
- Is it used in multiple places? If yes, prefer `Common_` / `Msg_`.
- If the string is a sentence shown to the user, prefer `Msg_`.
