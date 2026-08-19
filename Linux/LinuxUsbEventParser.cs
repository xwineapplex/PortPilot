using System;
using System.Collections.Generic;

namespace PortPilot_Project.Linux;

/// <summary>
/// Parse property blocks produced by <c>udevadm monitor</c>.
/// </summary>
internal sealed class LinuxUsbEventParser
{
    private readonly Dictionary<string, string> _currentEvent = new(StringComparer.Ordinal);
    private bool _inEvent;

    /// <summary>
    /// Consume one output line and return a completed event when available.
    /// </summary>
    internal IReadOnlyDictionary<string, string>? ReadLine(string line)
    {
        var trimmed = line.Trim();

        if (IsHeader(trimmed))
        {
            var completed = CompleteCurrentEvent();
            _inEvent = true;
            return completed;
        }

        if (trimmed.Length == 0)
            return CompleteCurrentEvent();

        if (!_inEvent)
            return null;

        var separatorIndex = trimmed.IndexOf('=');
        if (separatorIndex <= 0)
            return null;

        var key = trimmed[..separatorIndex];
        var value = trimmed[(separatorIndex + 1)..];
        _currentEvent[key] = value;
        return null;
    }

    /// <summary>
    /// Return a complete final event when the output stream reaches EOF.
    /// </summary>
    internal IReadOnlyDictionary<string, string>? Complete()
        => CompleteCurrentEvent();

    private static bool IsHeader(string line)
        => line.StartsWith("UDEV", StringComparison.Ordinal)
           || line.StartsWith("KERNEL", StringComparison.Ordinal);

    private IReadOnlyDictionary<string, string>? CompleteCurrentEvent()
    {
        if (!_inEvent)
            return null;

        _inEvent = false;

        IReadOnlyDictionary<string, string>? completed = null;
        if (_currentEvent.ContainsKey("ACTION") && _currentEvent.ContainsKey("DEVPATH"))
            completed = new Dictionary<string, string>(_currentEvent, StringComparer.Ordinal);

        _currentEvent.Clear();
        return completed;
    }
}
