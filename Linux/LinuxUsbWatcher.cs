using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using PortPilot_Project.Abstractions;
using PortPilot_Project.Properties;

namespace PortPilot_Project.Linux;

public sealed class LinuxUsbWatcher : IUsbWatcher
{
    private readonly object _lifecycleGate = new();
    private readonly Func<IUdevMonitorProcess> _startProcess;
    private readonly bool _isSupported;
    private IUdevMonitorProcess? _process;
    private CancellationTokenSource? _cts;
    private Task? _readTask;
    private Task? _stderrTask;
    private readonly Dictionary<string, UsbDeviceInfo> _knownDevices = new();
    private bool _disposed;

    public LinuxUsbWatcher()
        : this(UdevMonitorProcess.Start, OperatingSystem.IsLinux())
    {
    }

    internal LinuxUsbWatcher(Func<IUdevMonitorProcess> startProcess, bool isSupported)
    {
        _startProcess = startProcess;
        _isSupported = isSupported;
    }

    internal Task ReadCompletion => _readTask ?? Task.CompletedTask;

    public event EventHandler<UsbDeviceChangedEventArgs>? DeviceChanged;

    public void Start()
    {
        lock (_lifecycleGate)
        {
            if (_disposed || _process is not null || !_isSupported)
                return;

            var cts = new CancellationTokenSource();
            IUdevMonitorProcess? process = null;
            try
            {
                process = _startProcess();
                var readTask = Task.Run(() => ReadLoopAsync(process, cts.Token));
                var stderrTask = Task.Run(() => DrainStandardErrorAsync(process.StandardError, cts.Token));

                _process = process;
                _cts = cts;
                _readTask = readTask;
                _stderrTask = stderrTask;
            }
            catch
            {
                cts.Cancel();
                cts.Dispose();
                process?.Dispose();
                throw;
            }
        }
    }

    private async Task ReadLoopAsync(IUdevMonitorProcess process, CancellationToken token)
    {
        var parser = new LinuxUsbEventParser();

        try
        {
            while (await process.StandardOutput.ReadLineAsync(token) is { } line)
            {
                token.ThrowIfCancellationRequested();
                ProcessParsedEvent(parser.ReadLine(line));
            }

            if (!token.IsCancellationRequested)
                ProcessParsedEvent(parser.Complete());
        }
        catch (OperationCanceledException)
        {
            // Treat cancellation as the expected watcher shutdown path.
        }
        catch (ObjectDisposedException) when (token.IsCancellationRequested)
        {
            // Treat stream disposal during shutdown as expected.
        }
        catch (Exception ex)
        {
            Console.WriteLine(string.Format(
                CultureInfo.CurrentUICulture,
                Resources.Msg_Error_LinuxUsbWatcher,
                ex.Message));
        }
    }

    private static async Task DrainStandardErrorAsync(TextReader standardError, CancellationToken token)
    {
        try
        {
            while (await standardError.ReadLineAsync(token) is not null)
                token.ThrowIfCancellationRequested();
        }
        catch (OperationCanceledException)
        {
            // Treat cancellation as the expected watcher shutdown path.
        }
        catch (ObjectDisposedException) when (token.IsCancellationRequested)
        {
            // Treat stream disposal during shutdown as expected.
        }
        catch (Exception ex)
        {
            Console.WriteLine(string.Format(
                CultureInfo.CurrentUICulture,
                Resources.Msg_Error_LinuxUsbWatcher,
                ex.Message));
        }
    }

    private void ProcessParsedEvent(IReadOnlyDictionary<string, string>? properties)
    {
        if (properties is not null)
            ProcessEvent(properties);
    }

    private void ProcessEvent(IReadOnlyDictionary<string, string> properties)
    {
        if (!properties.TryGetValue("ACTION", out var action)) return;
        if (!properties.TryGetValue("DEVPATH", out var devPath)) return;

        // Filter by DEVTYPE to avoid interface duplicates.
        // Proceed when DEVTYPE is missing to avoid missing events.
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
            var deviceId = $"USB\\VID_{vid.ToUpperInvariant()}&PID_{pid.ToUpperInvariant()}\\{serial}";

            var info = new UsbDeviceInfo(deviceId, name, vid.ToUpperInvariant(), pid.ToUpperInvariant());
            
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
                    // Fall back to VID/PID matching when properties are available.
                    // Handle cases where the initial scan used a different path.
                    if (properties.TryGetValue("ID_VENDOR_ID", out var vid) &&
                        properties.TryGetValue("ID_MODEL_ID", out var pid))
                    {
                        var serial = properties.TryGetValue("ID_SERIAL_SHORT", out var s) ? s : null;
                        var vidUpper = vid.ToUpperInvariant();
                        var pidUpper = pid.ToUpperInvariant();

                        // Find matching device in known devices.
                        foreach (var kvp in _knownDevices)
                        {
                            if (kvp.Value.Vid == vidUpper && kvp.Value.Pid == pidUpper)
                            {
                                // Match by serial when available.
                                // Check serial in the known DeviceId when present.
                                if (serial != null && kvp.Value.DeviceId.Contains(serial))
                                {
                                    info = kvp.Value;
                                    keyToRemove = kvp.Key;
                                    break;
                                }
                                // When serial is unavailable, keep the first VID/PID match.
                                if (info == null)
                                {
                                    info = kvp.Value;
                                    keyToRemove = kvp.Key;
                                }
                            }
                        }

                        if (info == null)
                        {
                             // Fall back to reporting removal with current properties.
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
            // Use udevadm info --export-db to read the udev state.
            // Match DEVPATH with what udevadm monitor reports.
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

            // Split output into blocks separated by blank lines.
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

                // Require a valid path and a USB device.
                if (devPath == null) continue;
                
                // Check subsystem.
                if (!properties.TryGetValue("SUBSYSTEM", out var subsystem) || subsystem != "usb") continue;
                
                // Check devtype to avoid interfaces.
                if (properties.TryGetValue("DEVTYPE", out var devType) && devType != "usb_device") continue;

                // Require VID/PID.
                if (!properties.TryGetValue("ID_VENDOR_ID", out var vid)) continue;
                if (!properties.TryGetValue("ID_MODEL_ID", out var pid)) continue;

                var name = properties.TryGetValue("ID_MODEL", out var model) ? model : Resources.Common_Unknown;
                var vendor = properties.TryGetValue("ID_VENDOR", out var v) ? v : "";
                if (!string.IsNullOrEmpty(vendor)) name = $"{vendor} {name}";

                // Use DEVPATH from properties when available.
                if (properties.TryGetValue("DEVPATH", out var dp)) devPath = dp;

                var serial = properties.TryGetValue("ID_SERIAL_SHORT", out var s) ? s : devPath;
                var deviceId = $"USB\\VID_{vid.ToUpperInvariant()}&PID_{pid.ToUpperInvariant()}\\{serial}";

                var info = new UsbDeviceInfo(deviceId, name, vid.ToUpperInvariant(), pid.ToUpperInvariant());

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
        IUdevMonitorProcess? process;
        CancellationTokenSource? cts;
        Task? readTask;
        Task? stderrTask;

        lock (_lifecycleGate)
        {
            process = _process;
            cts = _cts;
            readTask = _readTask;
            stderrTask = _stderrTask;

            _process = null;
            _cts = null;
            _readTask = null;
            _stderrTask = null;
        }

        cts?.Cancel();

        if (process is not null)
        {
            try
            {
                if (!process.HasExited)
                    process.Kill(entireProcessTree: true);
            }
            catch
            {
                // Continue cleanup when the process exits between checks.
            }

            try { process.WaitForExit(TimeSpan.FromSeconds(2)); } catch { }
        }

        var tasks = new[] { readTask, stderrTask }.Where(task => task is not null).Cast<Task>().ToArray();
        if (tasks.Length > 0)
        {
            try { Task.WaitAll(tasks, TimeSpan.FromSeconds(2)); } catch { }
        }

        process?.Dispose();
        cts?.Dispose();

        lock (_knownDevices)
            _knownDevices.Clear();
    }

    public void Dispose()
    {
        lock (_lifecycleGate)
        {
            if (_disposed)
                return;

            _disposed = true;
        }

        Stop();
    }
}
