using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using PortPilot_Project.Abstractions;

namespace PortPilot_Project.Linux;

public sealed class LinuxMonitorController : IMonitorController
{
    public async Task<IReadOnlyList<MonitorInfo>> GetMonitorsAsync(CancellationToken cancellationToken = default)
    {
        if (!OperatingSystem.IsLinux())
            return Array.Empty<MonitorInfo>();

        var result = new List<MonitorInfo>();

        try
        {
            // Run ddcutil detect
            var output = await RunDdcUtilAsync("detect", cancellationToken);
            
            // Parse output
            // Example output:
            // Display 1
            //    I2C bus:  /dev/i2c-1
            //    Model:    DELL U2412M
            //    ...
            
            var lines = output.Split('\n');
            string? currentDisplay = null;
            string? currentModel = null;
            string? currentBus = null;

            foreach (var line in lines)
            {
                var trimmed = line.Trim();
                if (trimmed.StartsWith("Display "))
                {
                    if (currentDisplay != null && currentBus != null)
                    {
                        result.Add(new MonitorInfo(currentBus, currentModel ?? $"Display {currentDisplay}"));
                    }
                    
                    currentDisplay = trimmed.Substring("Display ".Length).Trim();
                    currentModel = null;
                    currentBus = null;
                }
                else if (trimmed.StartsWith("I2C bus:"))
                {
                    // /dev/i2c-1
                    var parts = trimmed.Split(':');
                    if (parts.Length > 1)
                    {
                        // We use the bus number as ID because it's more stable for ddcutil --bus
                        // Extract number from /dev/i2c-1 -> 1
                        var busPath = parts[1].Trim();
                        var match = Regex.Match(busPath, @"i2c-(\d+)");
                        if (match.Success)
                        {
                            currentBus = match.Groups[1].Value;
                        }
                    }
                }
                else if (trimmed.StartsWith("Model:"))
                {
                    var parts = trimmed.Split(':');
                    if (parts.Length > 1)
                    {
                        currentModel = parts[1].Trim();
                    }
                }
            }

            if (currentDisplay != null && currentBus != null)
            {
                result.Add(new MonitorInfo(currentBus, currentModel ?? $"Display {currentDisplay}"));
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error detecting monitors: {ex.Message}");
        }

        return result;
    }

    public async Task SetInputSourceAsync(string monitorId, ushort sourceCode, CancellationToken cancellationToken = default)
    {
        if (!OperatingSystem.IsLinux())
            return;

        try
        {
            // monitorId is the bus number
            // ddcutil setvcp 60 <value> --bus <bus>
            await RunDdcUtilAsync($"setvcp 60 {sourceCode} --bus {monitorId}", cancellationToken);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error setting input source: {ex.Message}");
        }
    }

    private async Task<string> RunDdcUtilAsync(string arguments, CancellationToken cancellationToken)
    {
        var startInfo = new ProcessStartInfo
        {
            FileName = "ddcutil",
            Arguments = arguments,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        using var process = new Process { StartInfo = startInfo };
        process.Start();

        var outputTask = process.StandardOutput.ReadToEndAsync(cancellationToken);
        var errorTask = process.StandardError.ReadToEndAsync(cancellationToken);

        await process.WaitForExitAsync(cancellationToken);

        if (process.ExitCode != 0)
        {
            var error = await errorTask;
            throw new Exception($"ddcutil exited with code {process.ExitCode}: {error}");
        }

        return await outputTask;
    }
}
