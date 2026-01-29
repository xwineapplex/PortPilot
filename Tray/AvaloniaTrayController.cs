using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Platform;
using Avalonia.Threading;
using PortPilot_Project.Abstractions;
using PortPilot_Project.Properties;
using PortPilot_Project.ViewModels;
using PortPilot_Project.Views;

namespace PortPilot_Project.Tray;

public sealed class AvaloniaTrayController : ITrayController
{
    private readonly Func<MainWindowViewModel> _getViewModel;
    private TrayIcon? _trayIcon;
    private NativeMenuItem? _monitoringMenuItem;

    public AvaloniaTrayController(Func<MainWindowViewModel> getViewModel)
    {
        _getViewModel = getViewModel;
    }

    public void Initialize()
    {
        if (Application.Current is null)
            return;

        if (Application.Current.ApplicationLifetime is not IClassicDesktopStyleApplicationLifetime)
            return;

        _trayIcon ??= CreateTrayIcon();
        _trayIcon.IsVisible = true;

        var vm = _getViewModel();
        vm.PropertyChanged += (_, e) =>
        {
            if (e.PropertyName == nameof(MainWindowViewModel.IsMonitoringEnabled))
            {
                Dispatcher.UIThread.Post(() =>
                {
                    if (_monitoringMenuItem is not null)
                    {
                        _monitoringMenuItem.IsChecked = vm.IsMonitoringEnabled;
                        _monitoringMenuItem.Header = GetMonitoringHeader(vm.IsMonitoringEnabled);
                    }
                });
            }
        };
    }

    private static string GetMonitoringHeader(bool enabled)
        => enabled ? Resources.Tray_Menu_MonitoringActive : Resources.Tray_Menu_MonitoringInactive;

    private TrayIcon CreateTrayIcon()
    {
        var vm = _getViewModel();

        var windowIcon = LoadTrayWindowIcon();
        var icon = new TrayIcon
        {
            ToolTipText = Resources.Tray_Tooltip_Running,
            Icon = windowIcon,
        };

        icon.Clicked += (_, __) => ShowWindow();

        var menu = new NativeMenu();

        var open = new NativeMenuItem(Resources.Tray_Menu_Open)
        {
            Command = vm.ShowWindowCommand,
        };

        _monitoringMenuItem = new NativeMenuItem(GetMonitoringHeader(vm.IsMonitoringEnabled))
        {
            ToggleType = NativeMenuItemToggleType.CheckBox,
            IsChecked = vm.IsMonitoringEnabled,
            Command = vm.ToggleMonitoringActiveCommand,
        };

        var exit = new NativeMenuItem(Resources.Tray_Menu_Exit)
        {
            Command = vm.ExitApplicationCommand,
        };

        menu.Items.Add(open);
        menu.Items.Add(_monitoringMenuItem);
        menu.Items.Add(new NativeMenuItemSeparator());
        menu.Items.Add(exit);

        icon.Menu = menu;
        return icon;
    }

    private static WindowIcon? LoadTrayWindowIcon()
    {
        try
        {
            var assemblyName = System.Reflection.Assembly.GetEntryAssembly()?.GetName().Name;
            var uri = new Uri($"avares://{assemblyName}/Assets/PortPilot.ico");
            using var stream = AssetLoader.Open(uri);
            return new WindowIcon(stream);
        }
        catch
        {
            return null;
        }
    }

    public void ShowWindow()
    {
        Dispatcher.UIThread.Post(() =>
        {
            var win = MainWindow.Current;
            if (win is null)
                return;

            if (!win.IsVisible)
                win.Show();

            win.WindowState = WindowState.Normal;
            win.Activate();
        });
    }

    public void HideWindow()
    {
        Dispatcher.UIThread.Post(() => MainWindow.Current?.Hide());
    }

    public void ExitApplication()
    {
        Dispatcher.UIThread.Post(() =>
        {
            if (Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
                desktop.Shutdown();
        });
    }

    public void Dispose()
    {
        if (_trayIcon is not null)
            _trayIcon.IsVisible = false;
        _trayIcon = null;
    }
}
