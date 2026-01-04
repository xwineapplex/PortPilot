using System;
using System.Collections.Generic;
using System.Management;
using System.Text.RegularExpressions;
using System.Runtime.Versioning;
using PortPilot_Project.Abstractions;

namespace PortPilot_Project.Windows;

[SupportedOSPlatform("windows")]
public sealed class WinUsbWatcher : IUsbWatcher
{
    private ManagementEventWatcher? _creationWatcher;
    private ManagementEventWatcher? _deletionWatcher;
    private bool _started;

    public event EventHandler<UsbDeviceChangedEventArgs>? DeviceChanged;

    public void Start()
    {
        if (_started)
            return;

        if (!OperatingSystem.IsWindows())
            return;

        // Captures a broad set of USB PnP events. Filtering by VID/PID can be done in app logic.
        _creationWatcher = new ManagementEventWatcher(new WqlEventQuery(
            "SELECT * FROM __InstanceCreationEvent WITHIN 1 WHERE TargetInstance ISA 'Win32_PnPEntity'"));
        _creationWatcher.EventArrived += (_, e) => Raise(e, UsbDeviceChangeType.Added);
        _creationWatcher.Start();

        _deletionWatcher = new ManagementEventWatcher(new WqlEventQuery(
            "SELECT * FROM __InstanceDeletionEvent WITHIN 1 WHERE TargetInstance ISA 'Win32_PnPEntity'"));
        _deletionWatcher.EventArrived += (_, e) => Raise(e, UsbDeviceChangeType.Removed);
        _deletionWatcher.Start();

        _started = true;
    }

    public List<UsbDeviceInfo> GetConnectedDevices()
    {
        var devices = new List<UsbDeviceInfo>();
        if (!OperatingSystem.IsWindows()) return devices;

        try
        {
            using var searcher = new ManagementObjectSearcher("SELECT * FROM Win32_PnPEntity WHERE DeviceID LIKE 'USB%'");
            foreach (var device in searcher.Get())
            {
                var deviceId = device["DeviceID"]?.ToString() ?? string.Empty;
                var name = device["Name"]?.ToString();
                var (vid, pid) = ParseVidPid(deviceId);

                if (vid != null && pid != null)
                {
                    devices.Add(new UsbDeviceInfo(deviceId, name, vid, pid));
                }
            }
        }
        catch
        {
            // Swallow exceptions
        }
        return devices;
    }

    public void Stop()
    {
        _started = false;

        if (_creationWatcher is not null)
        {
            try { _creationWatcher.Stop(); } catch { }
            _creationWatcher.Dispose();
            _creationWatcher = null;
        }

        if (_deletionWatcher is not null)
        {
            try { _deletionWatcher.Stop(); } catch { }
            _deletionWatcher.Dispose();
            _deletionWatcher = null;
        }
    }

    public void Dispose() => Stop();

    private void Raise(EventArrivedEventArgs e, UsbDeviceChangeType changeType)
    {
        try
        {
            if (e.NewEvent?["TargetInstance"] is not ManagementBaseObject instance)
                return;

            var deviceId = instance["DeviceID"]?.ToString() ?? string.Empty;
            var name = instance["Name"]?.ToString();

            var (vid, pid) = ParseVidPid(deviceId);

            var info = new UsbDeviceInfo(deviceId, name, vid, pid);
            DeviceChanged?.Invoke(this, new UsbDeviceChangedEventArgs(changeType, info));
        }
        catch
        {
            // Swallow watcher thread exceptions; app can log if needed.
        }
    }

    private static (string? Vid, string? Pid) ParseVidPid(string deviceId)
    {
        // Typical: USB\VID_046D&PID_C534\...
        var vidMatch = Regex.Match(deviceId, @"VID_([0-9A-Fa-f]{4})");
        var pidMatch = Regex.Match(deviceId, @"PID_([0-9A-Fa-f]{4})");
        return (
            vidMatch.Success ? vidMatch.Groups[1].Value.ToUpperInvariant() : null,
            pidMatch.Success ? pidMatch.Groups[1].Value.ToUpperInvariant() : null);
    }
}
