using PortPilot_Project.Config;

namespace PortPilot_Project.ViewModels;

public sealed record RuleDisplayItem(
    UsbTriggerRule Rule,
    string Vid,
    string Pid,
    string MonitorName,
    string OnAddedLabel,
    string OnRemovedLabel);
