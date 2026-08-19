using PortPilot_Project.Abstractions;

namespace PortPilot.Tests.TestDoubles;

internal sealed class FakeUsbWatcher : IUsbWatcher
{
    public event EventHandler<UsbDeviceChangedEventArgs>? DeviceChanged;

    public int StartCount { get; private set; }

    public int StopCount { get; private set; }

    public int DisposeCount { get; private set; }

    public List<UsbDeviceInfo> ConnectedDevices { get; } = new();

    public void Start()
        => StartCount++;

    public void Stop()
        => StopCount++;

    public List<UsbDeviceInfo> GetConnectedDevices()
        => ConnectedDevices.ToList();

    public void Raise(UsbDeviceChangeType changeType, UsbDeviceInfo device)
        => DeviceChanged?.Invoke(this, new UsbDeviceChangedEventArgs(changeType, device));

    public void Dispose()
        => DisposeCount++;
}
