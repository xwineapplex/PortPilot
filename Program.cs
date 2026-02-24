using System;
using System.Threading.Tasks;
using Avalonia;

namespace PortPilot_Project
{
    internal sealed class Program
    {
        // Avoid Avalonia, third-party APIs, and SynchronizationContext usage before AppMain.
        [STAThread]
        public static void Main(string[] args)
        {
            // Suppress TaskCanceledException from DBus teardown on Linux.
            // The DBus connection may try to dispatch via the Avalonia
            // SynchronizationContext after the Dispatcher has shut down.
            AppDomain.CurrentDomain.UnhandledException += (_, e) =>
            {
                if (e.ExceptionObject is TaskCanceledException)
                    return;
            };

            TaskScheduler.UnobservedTaskException += (_, e) =>
            {
                if (e.Exception.InnerException is TaskCanceledException)
                    e.SetObserved();
            };

            BuildAvaloniaApp().StartWithClassicDesktopLifetime(args);
        }

        // Keep Avalonia configuration for the visual designer.
        public static AppBuilder BuildAvaloniaApp()
            => AppBuilder.Configure<App>()
                .UsePlatformDetect()
                .WithInterFont()
                .LogToTrace();
    }
}
