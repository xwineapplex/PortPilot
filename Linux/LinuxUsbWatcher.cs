using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using PortPilot_Project.Abstractions;

namespace PortPilot_Project.Linux;

public sealed class LinuxUsbWatcher : IUsbWatcher
{
    private Process? _process;
    private CancellationTokenSource? _cts;
    private Task? _readTask;
    private readonly Dictionary<string, UsbDeviceInfo> _knownDevices = new();

    public event EventHandler<UsbDeviceChangedEventArgs>? DeviceChanged;

    public void Start()
    {
        if (_process != null)
            return;

        if (!OperatingSystem.IsLinux())
            return;

        _cts = new CancellationTokenSource();
        
        var startInfo = new ProcessStartInfo
        {
            FileName = "udevadm",
            Arguments = "monitor --udev --subsystem-match=usb --property",
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        _process = new Process { StartInfo = startInfo };
        _process.Start();

        _readTask = Task.Run(() => ReadLoop(_cts.Token));
    }

    private async Task ReadLoop(CancellationToken token)
    {
        if (_process == null) return;

        try
        {
            string? line;
            var currentEvent = new Dictionary<string, string>();
            bool inEvent = false;

            while ((line = await _process.StandardOutput.ReadLineAsync(token)) != null)
            {
                if (token.IsCancellationRequested) break;

                line = line.Trim();
                if (string.IsNullOrEmpty(line)) continue;

                // Header line starts with UDEV or KERNEL
                if (line.StartsWith("UDEV") || line.StartsWith("KERNEL"))
                {
                    // Process previous event if any
                    if (inEvent)
                    {
                        ProcessEvent(currentEvent);
                    }

                    // Start new event
                    currentEvent.Clear();
                    inEvent = true;
                    continue;
                }

                if (inEvent)
                {
                    var parts = line.Split('=', 2);
                    if (parts.Length == 2)
                    {
                        currentEvent[parts[0]] = parts[1];
                    }
                }
            }
        }
        catch (OperationCanceledException)
        {
            // Ignore
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error in LinuxUsbWatcher: {ex.Message}");
        }
    }

    private void ProcessEvent(Dictionary<string, string> properties)
    {
        if (!properties.TryGetValue("ACTION", out var action)) return;
        if (!properties.TryGetValue("DEVPATH", out var devPath)) return;

        // Filter by DEVTYPE to avoid duplicate events for interfaces if possible
        // If DEVTYPE is missing, we proceed (safer to process than to miss)
        if (properties.TryGetValue("DEVTYPE", out var devType) && devType != "usb_device")
            return;

        if (action == "add")
        {
            if (!properties.TryGetValue("ID_VENDOR_ID", out var vid)) return;
            if (!properties.TryGetValue("ID_MODEL_ID", out var pid)) return;

            var name = properties.TryGetValue("ID_MODEL", out var model) ? model : "Unknown";
            var vendor = properties.TryGetValue("ID_VENDOR", out var v) ? v : "";
            if (!string.IsNullOrEmpty(vendor)) name = $"{vendor} {name}";

            var serial = properties.TryGetValue("ID_SERIAL_SHORT", out var s) ? s : devPath;
            var deviceId = $"USB\\VID_{vid.ToUpper()}&PID_{pid.ToUpper()}\\{serial}";

            var info = new UsbDeviceInfo(deviceId, name, vid.ToUpper(), pid.ToUpper());
            
            _knownDevices[devPath] = info;
            DeviceChanged?.Invoke(this, new UsbDeviceChangedEventArgs(UsbDeviceChangeType.Added, info));
        }
        else if (action == "remove")
        {
            if (_knownDevices.TryGetValue(devPath, out var info))
            {
                _knownDevices.Remove(devPath);
                DeviceChanged?.Invoke(this, new UsbDeviceChangedEventArgs(UsbDeviceChangeType.Removed, info));
            }
        }
    }

    public void Stop()
    {
        _cts?.Cancel();
        
        if (_process != null && !_process.HasExited)
        {
            try
            {
                _process.Kill();
            }
            catch { }
        }

        _process?.Dispose();
        _process = null;
    }

    public void Dispose()
    {
        Stop();
    }
}
