using System;
using Avalonia;

namespace PortPilot_Project
{
    internal sealed class Program
    {
        // Avoid Avalonia, third-party APIs, and SynchronizationContext usage before AppMain.
        [STAThread]
        public static void Main(string[] args) => BuildAvaloniaApp()
            .StartWithClassicDesktopLifetime(args);

        // Keep Avalonia configuration for the visual designer.
        public static AppBuilder BuildAvaloniaApp()
            => AppBuilder.Configure<App>()
                .UsePlatformDetect()
                .WithInterFont()
                .LogToTrace();
    }
}
