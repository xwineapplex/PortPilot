using System.Linq;
using System.Globalization;
using System.Threading;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Data.Core;
using Avalonia.Data.Core.Plugins;
using Avalonia.Markup.Xaml;
using PortPilot_Project.Config;
using PortPilot_Project.Tray;
using PortPilot_Project.ViewModels;
using PortPilot_Project.Views;

namespace PortPilot_Project
{
    public partial class App : Application
    {
        private AvaloniaTrayController? _tray;

        public override void Initialize()
        {
            AvaloniaXamlLoader.Load(this);
        }

        private static void ApplyCultureFromConfig()
        {
            try
            {
                var config = new ConfigStore().LoadAsync().GetAwaiter().GetResult();
                var lang = (config.Language ?? "auto").Trim();

                if (string.IsNullOrWhiteSpace(lang) || string.Equals(lang, "auto", System.StringComparison.OrdinalIgnoreCase))
                {
                    PortPilot_Project.Properties.Resources.Culture = null;
                    return;
                }

                var culture = CultureInfo.GetCultureInfo(lang);

                CultureInfo.DefaultThreadCurrentCulture = culture;
                CultureInfo.DefaultThreadCurrentUICulture = culture;
                Thread.CurrentThread.CurrentCulture = culture;
                Thread.CurrentThread.CurrentUICulture = culture;

                PortPilot_Project.Properties.Resources.Culture = culture;
            }
            catch
            {
                // Ignore culture/config failures and fall back to system culture and neutral resources.
                PortPilot_Project.Properties.Resources.Culture = null;
            }
        }

        public override void OnFrameworkInitializationCompleted()
        {
            // Load config and apply culture before instantiating any UI.
            ApplyCultureFromConfig();

            if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
            {
                // Avoid duplicate validation between Avalonia and the CommunityToolkit.
                DisableAvaloniaDataAnnotationValidation();

                // Initialize the main window; the XAML parser runs here.
                var vm = new MainWindowViewModel();
                desktop.MainWindow = new MainWindow { DataContext = vm };

                desktop.MainWindow.Show();
                desktop.MainWindow.Activate();

                MainWindowViewModel.TrayAccess.ShowWindow = () => _tray?.ShowWindow();
                MainWindowViewModel.TrayAccess.ExitApplication = () => _tray?.ExitApplication();

                _tray = new AvaloniaTrayController(() => vm);
                _tray.Initialize();

                desktop.Exit += (_, __) => _tray?.Dispose();
            }

            base.OnFrameworkInitializationCompleted();
        }

        private void DisableAvaloniaDataAnnotationValidation()
        {
            // Get data validation plugins to remove.
            var dataValidationPluginsToRemove =
                BindingPlugins.DataValidators.OfType<DataAnnotationsValidationPlugin>().ToArray();

            // Remove each plugin found.
            foreach (var plugin in dataValidationPluginsToRemove)
            {
                BindingPlugins.DataValidators.Remove(plugin);
            }
        }
    }
}