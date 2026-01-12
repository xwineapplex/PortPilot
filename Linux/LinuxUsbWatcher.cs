using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Threading;
using System.Threading.Tasks;
using PortPilot_Project.Abstractions;
using PortPilot_Project.Properties;

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
            Console.WriteLine(string.Format(CultureInfo.CurrentUICulture, Resources.Msg_Error_LinuxUsbWatcher, ex.Message));
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

            var name = properties.TryGetValue("ID_MODEL", out var model) ? model : Resources.Common_Unknown;
            var vendor = properties.TryGetValue("ID_VENDOR", out var v) ? v : "";
            if (!string.IsNullOrEmpty(vendor)) name = $"{vendor} {name}";

            var serial = properties.TryGetValue("ID_SERIAL_SHORT", out var s) ? s : devPath;
            var deviceId = $"USB\\VID_{vid.ToUpper()}&PID_{pid.ToUpper()}\\{serial}";

            var info = new UsbDeviceInfo(deviceId, name, vid.ToUpper(), pid.ToUpper());
            
            lock (_knownDevices)
            {
                _knownDevices[devPath] = info;
            }
            DeviceChanged?.Invoke(this, new UsbDeviceChangedEventArgs(UsbDeviceChangeType.Added, info));
        }
        else if (action == "remove")
        {
            UsbDeviceInfo? info = null;
            string? keyToRemove = null;

            lock (_knownDevices)
            {
                if (_knownDevices.TryGetValue(devPath, out var found))
                {
                    info = found;
                    keyToRemove = devPath;
                }
                else
                {
                    // Fallback: Try to match by content if properties are available
                    // This handles cases where the initial scan registered the device with a slightly different path
                    if (properties.TryGetValue("ID_VENDOR_ID", out var vid) &&
                        properties.TryGetValue("ID_MODEL_ID", out var pid))
                    {
                        var serial = properties.TryGetValue("ID_SERIAL_SHORT", out var s) ? s : null;
                        var vidUpper = vid.ToUpper();
                        var pidUpper = pid.ToUpper();

                        // Try to find a matching device in known devices
                        foreach (var kvp in _knownDevices)
                        {
                            if (kvp.Value.Vid == vidUpper && kvp.Value.Pid == pidUpper)
                            {
                                // If we have a serial, match it. If not, we might be guessing but it's better than missing the event.
                                // If the known device has a serial in its DeviceId, we should check it.
                                if (serial != null && kvp.Value.DeviceId.Contains(serial))
                                {
                                    info = kvp.Value;
                                    keyToRemove = kvp.Key;
                                    break;
                                }
                                // If we don't have serial in event, or known device doesn't have it...
                                // If we only have one device with this VID/PID, assume it's the one.
                                if (info == null)
                                {
                                    info = kvp.Value;
                                    keyToRemove = kvp.Key;
                                }
                                else
                                {
                                    // Ambiguous match (multiple devices with same VID/PID), and we couldn't match serial.
                                    // If we found a second match, and we haven't confirmed by serial, maybe we should be careful.
                                    // But for now, let's just take the first one or the one we found.
                                }
                            }
                        }

                        if (info == null)
                        {
                             // Still not found, but we have enough info to report a removal
                             var name = properties.TryGetValue("ID_MODEL", out var model) ? model : Resources.Common_Unknown;
                             var vendor = properties.TryGetValue("ID_VENDOR", out var v) ? v : "";
                             if (!string.IsNullOrEmpty(vendor)) name = $"{vendor} {name}";
                             
                             var serialForId = serial ?? devPath;
                             var deviceId = $"USB\\VID_{vidUpper}&PID_{pidUpper}\\{serialForId}";
                             
                             info = new UsbDeviceInfo(deviceId, name, vidUpper, pidUpper);
                        }
                    }
                }

                if (keyToRemove != null)
                {
                    _knownDevices.Remove(keyToRemove);
                }
            }

            if (info != null)
            {
                DeviceChanged?.Invoke(this, new UsbDeviceChangedEventArgs(UsbDeviceChangeType.Removed, info));
            }
        }
    }

    public List<UsbDeviceInfo> GetConnectedDevices()
    {
        var devices = new List<UsbDeviceInfo>();
        if (!OperatingSystem.IsLinux()) return devices;

        try
        {
            // Use udevadm info --export-db to get the exact state as udev sees it.
            // This ensures DEVPATH matches what udevadm monitor reports.
            var startInfo = new ProcessStartInfo
            {
                FileName = "udevadm",
                Arguments = "info --export-db",
                RedirectStandardOutput = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            using var process = Process.Start(startInfo);
            if (process == null) return devices;

            var output = process.StandardOutput.ReadToEnd();
            process.WaitForExit();

            // Output is blocks separated by blank lines
            var blocks = output.Split(new[] { "\n\n" }, StringSplitOptions.RemoveEmptyEntries);
            
            foreach (var block in blocks)
            {
                var lines = block.Split('\n');
                var properties = new Dictionary<string, string>();
                string? devPath = null;

                foreach (var line in lines)
                {
                    if (line.StartsWith("P: "))
                    {
                        devPath = line.Substring(3).Trim();
                    }
                    else if (line.StartsWith("E: "))
                    {
                        var parts = line.Substring(3).Split('=', 2);
                        if (parts.Length == 2)
                        {
                            properties[parts[0]] = parts[1];
                        }
                    }
                }

                // We need a valid path and it must be a USB device
                if (devPath == null) continue;
                
                // Check subsystem
                if (!properties.TryGetValue("SUBSYSTEM", out var subsystem) || subsystem != "usb") continue;
                
                // Check devtype to avoid interfaces
                if (properties.TryGetValue("DEVTYPE", out var devType) && devType != "usb_device") continue;

                // Must have VID/PID
                if (!properties.TryGetValue("ID_VENDOR_ID", out var vid)) continue;
                if (!properties.TryGetValue("ID_MODEL_ID", out var pid)) continue;

                var name = properties.TryGetValue("ID_MODEL", out var model) ? model : Resources.Common_Unknown;
                var vendor = properties.TryGetValue("ID_VENDOR", out var v) ? v : "";
                if (!string.IsNullOrEmpty(vendor)) name = $"{vendor} {name}";

                // Use DEVPATH from properties if available, as that's what monitor uses
                if (properties.TryGetValue("DEVPATH", out var dp)) devPath = dp;

                var serial = properties.TryGetValue("ID_SERIAL_SHORT", out var s) ? s : devPath;
                var deviceId = $"USB\\VID_{vid.ToUpper()}&PID_{pid.ToUpper()}\\{serial}";

                var info = new UsbDeviceInfo(deviceId, name, vid.ToUpper(), pid.ToUpper());

                lock (_knownDevices)
                {
                    if (!_knownDevices.ContainsKey(devPath))
                    {
                        _knownDevices[devPath] = info;
                    }
                }
                devices.Add(info);
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine(string.Format(CultureInfo.CurrentUICulture, Resources.Msg_Error_UdevadmScanFailed, ex.Message));
        }
        return devices;
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
