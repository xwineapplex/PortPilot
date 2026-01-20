# PortPilot Comment Style Guide

Version: v1

## Purpose
Define consistent, English-only comment rules for the PortPilot codebase.

## Scope
- Apply to code comments only.
- Cover public and internal APIs by default.
- Private members are excluded unless clarity requires a comment.
- String resources follow the naming rules in docs/NAMING_CONVENTION.md.

## General Rules
- Write comments in English.
- Allow original UI strings and proper nouns when needed.
- Use imperative mood with present tense and a verb-first structure.
- Avoid a subject in summaries (avoid "This class..." or "This method...").
- Avoid stating obvious code behavior.
- Do not describe history or change logs in comments.
- Do not include external links in comments.
- Keep comment lines at or below 100 characters.
- When a comment changes, update any adjacent summary to keep it accurate.

## XML Documentation Comments (C#)
- Required for public and internal types and members unless an exception applies.
- Use a single sentence for `summary`, `param`, and `returns`.
- Start with a present-tense verb and keep an imperative tone.
- Include units only when needed for clarity.
- Use `remarks` only when `summary` cannot capture essential constraints.
- Limit `remarks` to 1–2 sentences and avoid bullet lists.
- Use `example` only for public APIs.
- Do not include external links in XML doc comments.

### XML Doc Examples
```csharp
/// <summary>
/// Return the sum of two integers.
/// </summary>
/// <param name="a">Provide the first operand.</param>
/// <param name="b">Provide the second operand.</param>
/// <returns>Return the computed sum.</returns>
public int Add(int a, int b) => a + b;
```

```csharp
/// <summary>
/// Load configuration from disk.
/// </summary>
/// <remarks>
/// Use this method only when the config file exists.
/// </remarks>
public AppConfig LoadConfig() => ...;
```

```csharp
/// <summary>
/// Format a monitor label for display.
/// </summary>
/// <example>
/// <code>
/// var label = FormatLabel("Dell", "HDMI1");
/// </code>
/// </example>
public string FormatLabel(string brand, string input) => ...;
```

## XAML Comments
- Write in English.
- Limit to 1–2 sentences.
- Use imperative, verb-first phrasing.
- Focus on layout intent or non-obvious styling decisions.
- Do not describe history or include external links.

### XAML Example
```xml
<!-- Use a dynamic system brush to follow light/dark themes. -->
```

## TODO/FIXME
- Allowed in comments.
- Write in English and keep each line at or below 100 characters.
- Keep items actionable and specific.

Example:
```csharp
// TODO: Add cancellation support for long-running scans.
```

## Exceptions (When Summary Is Optional)
1. Self-explanatory public or internal APIs.
2. Delegates or pass-through wrappers that add no behavior.
3. Generated or auto-maintained code.
4. Interface implementations or overrides where the meaning is inherited.
5. Enums and constants with unambiguous naming.
6. Private members (outside the required scope).
7. XAML layouts that are obvious from structure.
8. Test code where test names already explain intent.
9. Rapidly changing code where only a high-level comment is practical.
10. Security- or performance-sensitive areas where details should remain abstract.
