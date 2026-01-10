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

        try
        {
            // Captures a broad set of USB PnP events using WMI.
            // polling interval (WITHIN 1) means it checks every 1 second.
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
        catch (Exception)
        {
            // Stop and rethrow to let the caller (ViewModel) handle or log the start failure.
            Stop();
            throw;
        }
    }

    public List<UsbDeviceInfo> GetConnectedDevices()
    {
        var devices = new List<UsbDeviceInfo>();
        if (!OperatingSystem.IsWindows()) return devices;

        // Removed the internal try-catch swallowing. 
        // Any WMI errors will now bubble up to InitializeAsync in the ViewModel where they are logged.
        // Added 'Present = TRUE' to ensure we only get currently plugged-in devices.
        using var searcher = new ManagementObjectSearcher("SELECT * FROM Win32_PnPEntity WHERE DeviceID LIKE 'USB%' AND Present = TRUE");
        using var collection = searcher.Get();

        foreach (var device in collection)
        {
            var deviceId = device["DeviceID"]?.ToString() ?? string.Empty;
            var name = device["Name"]?.ToString();
            var (vid, pid) = ParseVidPid(deviceId);

            // Only add devices where we successfully parsed VIP/PID
            if (vid != null && pid != null)
            {
                devices.Add(new UsbDeviceInfo(deviceId, name, vid, pid));
            }
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

            // Optimization: Filter out events for internal hubs/controllers that don't have VID/PID
            if (vid == null || pid == null)
                return;

            var info = new UsbDeviceInfo(deviceId, name, vid, pid);
            DeviceChanged?.Invoke(this, new UsbDeviceChangedEventArgs(changeType, info));
        }
        catch
        {
            // Swallow watcher thread exceptions to prevent crashing the application background thread
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
