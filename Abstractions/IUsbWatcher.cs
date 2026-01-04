using System;
using System.Collections.Generic;

namespace PortPilot_Project.Abstractions;

public interface IUsbWatcher : IDisposable
{
    event EventHandler<UsbDeviceChangedEventArgs>? DeviceChanged;
    void Start();
    void Stop();
    List<UsbDeviceInfo> GetConnectedDevices();
}
