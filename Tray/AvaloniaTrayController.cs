using System;
using System.IO;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Platform;
using Avalonia.Threading;
using PortPilot_Project.Abstractions;
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
        => enabled ? "Monitoring Active (ºÊ±±¤¤)" : "Monitoring Inactive (¼È°±¤¤)";

    private TrayIcon CreateTrayIcon()
    {
        var vm = _getViewModel();

        var windowIcon = LoadTrayWindowIcon();
        var icon = new TrayIcon
        {
            ToolTipText = "PortPilot",
            Icon = windowIcon,
        };

        icon.Clicked += (_, __) => ShowWindow();

        var menu = new NativeMenu();

        var open = new NativeMenuItem("Open PortPilot")
        {
            Command = vm.ShowWindowCommand,
        };

        _monitoringMenuItem = new NativeMenuItem(GetMonitoringHeader(vm.IsMonitoringEnabled))
        {
            ToggleType = NativeMenuItemToggleType.CheckBox,
            IsChecked = vm.IsMonitoringEnabled,
            Command = vm.ToggleMonitoringActiveCommand,
        };

        var exit = new NativeMenuItem("Exit")
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
            var iconPath = EnsureTrayIconFile();
            return new WindowIcon(iconPath);
        }
        catch
        {
            return null;
        }
    }

    private static string EnsureTrayIconFile()
    {
        var baseDir = AppContext.BaseDirectory;
        var path = Path.Combine(baseDir, "tray.ico");
        if (File.Exists(path))
            return path;

        var uri = new Uri("avares://PortPilot_Project/Assets/avalonia-logo.ico");
        using var stream = AssetLoader.Open(uri);
        using var file = File.Create(path);
        stream.CopyTo(file);
        return path;
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
