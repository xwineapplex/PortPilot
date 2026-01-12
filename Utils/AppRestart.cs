using System;
using System.Diagnostics;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;

namespace PortPilot_Project.Utils;

public static class AppRestart
{
    public static void RestartApplication()
    {
        var processPath = Environment.ProcessPath;
        if (!string.IsNullOrWhiteSpace(processPath))
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = processPath,
                UseShellExecute = true,
            });
        }

        if (Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
            desktop.Shutdown();
        else
            Environment.Exit(0);
    }

    public static bool TryRestart()
    {
        try
        {
            RestartApplication();
            return true;
        }
        catch
        {
            return false;
        }
    }
}
