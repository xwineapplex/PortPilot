using System.Linq;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Data.Core;
using Avalonia.Data.Core.Plugins;
using Avalonia.Markup.Xaml;
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

        public override void OnFrameworkInitializationCompleted()
        {
            if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
            {
                // Avoid duplicate validations from both Avalonia and the CommunityToolkit. 
                // More info: https://docs.avaloniaui.net/docs/guides/development-guides/data-validation#manage-validationplugins
                DisableAvaloniaDataAnnotationValidation();

                var vm = new MainWindowViewModel();

                desktop.MainWindow = new MainWindow
                {
                    DataContext = vm,
                };

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
            // Get an array of plugins to remove
            var dataValidationPluginsToRemove =
                BindingPlugins.DataValidators.OfType<DataAnnotationsValidationPlugin>().ToArray();

            // remove each entry found
            foreach (var plugin in dataValidationPluginsToRemove)
            {
                BindingPlugins.DataValidators.Remove(plugin);
            }
        }
    }
}