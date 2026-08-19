using System;
using System.Diagnostics;
using System.IO;

namespace PortPilot_Project.Linux;

/// <summary>
/// Provide the process operations required by the Linux USB watcher.
/// </summary>
internal interface IUdevMonitorProcess : IDisposable
{
    TextReader StandardOutput { get; }

    TextReader StandardError { get; }

    bool HasExited { get; }

    void Kill(bool entireProcessTree);

    bool WaitForExit(TimeSpan timeout);
}

/// <summary>
/// Adapt a system process for Linux USB monitoring.
/// </summary>
internal sealed class UdevMonitorProcess : IUdevMonitorProcess
{
    private readonly Process _process;

    private UdevMonitorProcess(Process process)
    {
        _process = process;
    }

    public TextReader StandardOutput => _process.StandardOutput;

    public TextReader StandardError => _process.StandardError;

    public bool HasExited => _process.HasExited;

    /// <summary>
    /// Start <c>udevadm monitor</c> with redirected output streams.
    /// </summary>
    internal static IUdevMonitorProcess Start()
    {
        var startInfo = new ProcessStartInfo
        {
            FileName = "udevadm",
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true,
        };
        startInfo.ArgumentList.Add("monitor");
        startInfo.ArgumentList.Add("--udev");
        startInfo.ArgumentList.Add("--subsystem-match=usb");
        startInfo.ArgumentList.Add("--property");

        var process = new Process { StartInfo = startInfo };
        try
        {
            if (!process.Start())
                throw new InvalidOperationException("Failed to start udevadm monitor.");

            return new UdevMonitorProcess(process);
        }
        catch
        {
            process.Dispose();
            throw;
        }
    }

    public void Kill(bool entireProcessTree)
        => _process.Kill(entireProcessTree);

    public bool WaitForExit(TimeSpan timeout)
        => _process.WaitForExit(timeout);

    public void Dispose()
        => _process.Dispose();
}
