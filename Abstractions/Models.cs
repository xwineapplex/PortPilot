namespace PortPilot_Project.Abstractions;

public sealed record MonitorInfo(string Id, string? Name);

public sealed record UsbDeviceInfo(string DeviceId, string? Name, string? Vid, string? Pid);

public sealed class UsbDeviceChangedEventArgs : System.EventArgs
{
    public UsbDeviceChangedEventArgs(UsbDeviceChangeType changeType, UsbDeviceInfo device)
    {
        ChangeType = changeType;
        Device = device;
    }

    public UsbDeviceChangeType ChangeType { get; }
    public UsbDeviceInfo Device { get; }
}

public enum UsbDeviceChangeType
{
    Added,
    Removed,
}
