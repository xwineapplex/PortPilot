using System;
using System.Diagnostics;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;

namespace PortPilot_Project.Utils;

public static class AppRestart
{
    public static bool TryRestart()
    {
        try
        {
            var processPath = Environment.ProcessPath;
            if (string.IsNullOrWhiteSpace(processPath))
                return false;

            Process.Start(new ProcessStartInfo
            {
                FileName = processPath,
                UseShellExecute = true,
            });

            if (Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
                desktop.Shutdown();

            return true;
        }
        catch
        {
            return false;
        }
    }
}
