using System.Collections.Generic;

namespace PortPilot_Project.Config;

public sealed class AppConfig
{
    public List<UsbTriggerRule> Rules { get; set; } = new();

    /// <summary>
    /// Specify UI language as "auto", "en-US", or "zh-Hant".
    /// </summary>
    public string Language { get; set; } = "auto";

    // Store last selections for convenience.
    public string? LastSelectedMonitorId { get; set; }
    public ushort? LastInputSource { get; set; }

    public bool MinimizeToTrayOnClose { get; set; } = true;

    // Persist whether USB monitoring is enabled.
    public bool MonitoringEnabled { get; set; } = true;
}

public sealed class UsbTriggerRule
{
    public string? Vid { get; set; }
    public string? Pid { get; set; }

    // Use explicit actions per event type.
    public UsbEventAction? OnAdded { get; set; }
    public UsbEventAction? OnRemoved { get; set; }

    // Support older config shape for backward compatibility.
    public UsbTriggerAction? Action { get; set; }

    public bool Matches(string? vid, string? pid)
    {
        if (string.IsNullOrWhiteSpace(Vid) || string.IsNullOrWhiteSpace(Pid))
            return false;
        return string.Equals(Vid, vid, System.StringComparison.OrdinalIgnoreCase)
            && string.Equals(Pid, pid, System.StringComparison.OrdinalIgnoreCase);
    }

    public UsbEventAction? GetActionFor(PortPilot_Project.Abstractions.UsbDeviceChangeType changeType)
    {
        return changeType switch
        {
            PortPilot_Project.Abstractions.UsbDeviceChangeType.Added => OnAdded ?? (Action?.TriggerOnAdded == true ? new UsbEventAction { MonitorId = Action.MonitorId, InputSource = Action.InputSource } : null),
            PortPilot_Project.Abstractions.UsbDeviceChangeType.Removed => OnRemoved ?? (Action?.TriggerOnRemoved == true ? new UsbEventAction { MonitorId = Action.MonitorId, InputSource = Action.InputSource } : null),
            _ => null
        };
    }
}

public sealed class UsbEventAction
{
    public string? MonitorId { get; set; }
    public ushort InputSource { get; set; }
}

// Represent legacy config shape for backward compatibility.
public sealed class UsbTriggerAction
{
    public string? MonitorId { get; set; }
    public ushort InputSource { get; set; }

    public bool TriggerOnAdded { get; set; } = true;
    public bool TriggerOnRemoved { get; set; } = false;
}
