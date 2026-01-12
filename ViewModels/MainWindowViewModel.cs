using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using PortPilot_Project.Abstractions;
using PortPilot_Project.Config;
using PortPilot_Project.Models;
using PortPilot_Project.Properties;
using PortPilot_Project.Windows;
using PortPilot_Project.Linux;
using PortPilot_Project.Views;

namespace PortPilot_Project.ViewModels;

public partial class MainWindowViewModel : ViewModelBase
{
    private readonly IMonitorController _monitorController;
    private readonly IUsbWatcher _usbWatcher;
    private readonly ConfigStore _configStore;

    private AppConfig _config = new();

    public ObservableCollection<MonitorInfo> Monitors { get; } = new();
    public ObservableCollection<UsbDeviceInfo> RecentUsbEvents { get; } = new();
    public ObservableCollection<UsbTriggerRule> Rules { get; } = new();
    private readonly LinkedList<UsbDeviceInfo> _recentUsbForTargets = new();
    private const int RecentUsbForTargetsLimit = 50;

    [ObservableProperty]
    private bool isDebugMode;

    // Replace the monitoring flag ObservableProperty + partial hook with an explicit property.

    private bool _isMonitoringEnabled;
    public bool IsMonitoringEnabled
    {
        get => _isMonitoringEnabled;
        set
        {
            if (SetProperty(ref _isMonitoringEnabled, value))
            {
                _ = Dispatcher.UIThread.InvokeAsync(async () =>
                {
                    await SetMonitoringEnabledAsync(value);
                });
            }
        }
    }

    public ObservableCollection<string> DebugLog { get; } = new();

    public string DebugLogText => GetDebugLogText();

    // New UI selections for connect/disconnect actions
    [ObservableProperty]
    private InputSourceOption? onAddedInputSourceOption;

    [ObservableProperty]
    private InputSourceOption? onRemovedInputSourceOption;

    [ObservableProperty]
    private bool showAllUsbEvents;

    public ObservableCollection<UsbDeviceInfo> UsbTargets { get; } = new();

    private void Log(string message)
    {
        if (!IsDebugMode)
            return;

        var line = $"[{DateTime.Now:HH:mm:ss.fff}] {message}";
        // Ensure UI-thread collection updates.
        Dispatcher.UIThread.Post(() =>
        {
            DebugLog.Insert(0, line);
            if (DebugLog.Count > 200)
                DebugLog.RemoveAt(DebugLog.Count - 1);

            OnPropertyChanged(nameof(DebugLogText));
        });
    }

    [ObservableProperty]
    private MonitorInfo? selectedMonitor;

    [ObservableProperty]
    private UsbDeviceInfo? selectedUsbDevice;

    partial void OnSelectedMonitorChanged(MonitorInfo? value)
    {
        Dispatcher.UIThread.Post(() => AddRuleFromSelectionCommand.NotifyCanExecuteChanged());
    }

    partial void OnSelectedUsbDeviceChanged(UsbDeviceInfo? value)
    {
        Dispatcher.UIThread.Post(() => AddRuleFromSelectionCommand.NotifyCanExecuteChanged());
    }

    public IReadOnlyList<InputSourceOption> InputSourceOptions { get; } = new[]
    {
        new InputSourceOption(Resources.Enum_InputSource_DisplayPort1, 0x0F),
        new InputSourceOption(Resources.Enum_InputSource_DisplayPort2, 0x10),
        new InputSourceOption(Resources.Enum_InputSource_HDMI1, 0x11),
        new InputSourceOption(Resources.Enum_InputSource_HDMI2, 0x12),
        new InputSourceOption(Resources.Enum_InputSource_VGA, 0x01),
    };

    [ObservableProperty]
    private InputSourceOption? selectedInputSourceOption;

    [ObservableProperty]
    private ushort inputSource;

    [ObservableProperty]
    private string status = "";

    partial void OnShowAllUsbEventsChanged(bool value)
    {
        DiffUpdateUsbTargets(null);
    }

    public MainWindowViewModel()
    {
        if (OperatingSystem.IsWindows())
        {
            _monitorController = new WinMonitorController();
            _usbWatcher = new WinUsbWatcher();
        }
        else if (OperatingSystem.IsLinux())
        {
            _monitorController = new LinuxMonitorController();
            _usbWatcher = new LinuxUsbWatcher();
        }
        else
        {
            _monitorController = new NullMonitorController();
            _usbWatcher = new NullUsbWatcher();
        }

        _configStore = new ConfigStore();

        Log("VM constructed");
         _ = InitializeAsync();
     }

    private async Task InitializeAsync()
    {
        Log("InitializeAsync start");
        await LoadConfigAsync();

        _usbWatcher.DeviceChanged += (_, e) =>
        {
            // Hard reject: when monitoring is disabled, ignore any late/racing events.
            if (!IsMonitoringEnabled)
                return;

            // WMI events come from a non-UI thread.
            Dispatcher.UIThread.Post(async () =>
            {
                // Double-check after marshaling to UI thread.
                if (!IsMonitoringEnabled)
                    return;

                Log($"USB {e.ChangeType} Name='{e.Device.Name}' Vid={e.Device.Vid ?? "null"} Pid={e.Device.Pid ?? "null"}");
                Log($"USB DeviceId='{e.Device.DeviceId}'");

                // Only keep the raw event list in debug mode.
                if (IsDebugMode)
                    RecentUsbEvents.Insert(0, e.Device);

                // Update targets immediately (no debounce).
                DiffUpdateUsbTargets(e.Device);

                var deviceLabel = e.Device.Name ?? e.Device.DeviceId;
                Status = e.ChangeType == UsbDeviceChangeType.Added
                    ? string.Format(CultureInfo.CurrentUICulture, Resources.Msg_DeviceConnected, deviceLabel)
                    : string.Format(CultureInfo.CurrentUICulture, Resources.Msg_DeviceDisconnected, deviceLabel);

                await ApplyRulesAsync(e.ChangeType, e.Device);
            });
        };

        await RefreshMonitorsAsync();

        // Ensure we have usable defaults before any rule creation.
        if (SelectedMonitor is null && Monitors.Count > 0)
            SelectedMonitor = Monitors[0];
        if (InputSource == 0)
            InputSource = InputSourceOptions.FirstOrDefault()?.Code ?? (ushort)0x0F;

        // Restore last UI selections.
        if (!string.IsNullOrWhiteSpace(_config.LastSelectedMonitorId))
            SelectedMonitor = Monitors.FirstOrDefault(m => string.Equals(m.Id, _config.LastSelectedMonitorId, StringComparison.OrdinalIgnoreCase));
        if (_config.LastInputSource is ushort last)
            InputSource = last;

        SelectedInputSourceOption = InputSourceOptions.FirstOrDefault(o => o.Code == InputSource) ?? InputSourceOptions.FirstOrDefault();
        OnAddedInputSourceOption = SelectedInputSourceOption;
        OnRemovedInputSourceOption = InputSourceOptions.FirstOrDefault(o => o.Code != (OnAddedInputSourceOption?.Code ?? InputSource))
                                   ?? InputSourceOptions.Skip(1).FirstOrDefault()
                                   ?? InputSourceOptions.FirstOrDefault();

        // Apply persisted monitoring state last (after config load).
        IsMonitoringEnabled = _config.MonitoringEnabled;

        Log($"InitializeAsync done: Monitors={Monitors.Count}, SelectedMonitorId='{SelectedMonitor?.Id ?? "null"}', InputSource=0x{InputSource:X2}");
    }

    private async Task SetMonitoringEnabledAsync(bool enabled)
    {
        if (enabled)
        {
            try
            {
                // Reset / restart semantics.
                _usbWatcher.Stop();

                // Reset history then repopulate from a single scan (for UI list correction only).
                _recentUsbForTargets.Clear();

                // Also rebuild the UI list from scratch to reflect the scan.
                UsbTargets.Clear();

                try
                {
                    var initialDevices = _usbWatcher.GetConnectedDevices();
                    foreach (var device in initialDevices)
                    {
                        if (IsDebugMode)
                            RecentUsbEvents.Insert(0, device);
                        DiffUpdateUsbTargets(device);
                    }
                    Log($"Enable scan found {initialDevices.Count} devices");
                }
                catch (Exception ex)
                {
                    Log($"Enable scan failed: {ex}");
                }

                _usbWatcher.Start();
                Status = Resources.Msg_Status_MonitoringStarted;
                Log("USB watcher started (enabled)");
            }
            catch (Exception ex)
            {
                Status = string.Format(CultureInfo.CurrentUICulture, Resources.Msg_Error_Prefix, ex.Message);
                Log($"USB watcher start failed: {ex}");

                // Revert UI state on failure.
                if (IsMonitoringEnabled)
                    IsMonitoringEnabled = false;
            }
        }
        else
        {
            try
            {
                // Start -> Stop semantics (Stop is idempotent).
                _usbWatcher.Stop();
                Status = Resources.Msg_Status_MonitoringStopped;
                Log("USB watcher stopped (disabled)");
            }
            catch (Exception ex)
            {
                Status = string.Format(CultureInfo.CurrentUICulture, Resources.Msg_Error_Prefix, ex.Message);
                Log($"USB watcher stop failed: {ex}");
            }
        }

        // Persist immediately, but keep Status as the service state.
        _config.MonitoringEnabled = enabled;
        await SaveConfigAsync(updateStatus: false);
    }

    private async Task LoadConfigAsync()
    {
        try
        {
            _config = await _configStore.LoadAsync();
            Rules.Clear();
            foreach (var r in _config.Rules)
                Rules.Add(r);

            OnPropertyChanged(nameof(MinimizeToTrayOnClose));
            OnPropertyChanged(nameof(RulesDisplay));
        }
        catch
        {
            Status = Resources.Msg_Error_ConfigLoadFailed;
            _config = new AppConfig();
            Rules.Clear();

            OnPropertyChanged(nameof(MinimizeToTrayOnClose));
            OnPropertyChanged(nameof(RulesDisplay));
        }
    }

    public IReadOnlyList<RuleDisplayItem> RulesDisplay => Rules
        .Select(r => new RuleDisplayItem(
            r,
            r.Vid ?? string.Empty,
            r.Pid ?? string.Empty,
            ResolveMonitorName(r.OnAdded?.MonitorId ?? r.OnRemoved?.MonitorId),
            ResolveInputLabel(r.OnAdded?.InputSource),
            ResolveInputLabel(r.OnRemoved?.InputSource)))
        .ToList();

    private string ResolveMonitorName(string? monitorId)
    {
        if (string.IsNullOrWhiteSpace(monitorId))
            return "";
        return Monitors.FirstOrDefault(m => string.Equals(m.Id, monitorId, StringComparison.OrdinalIgnoreCase))?.Name
            ?? monitorId;
    }

    private string ResolveInputLabel(ushort? code)
    {
        if (code is null)
            return "";
        return InputSourceOptions.FirstOrDefault(o => o.Code == code.Value)?.Name
            ?? $"0x{code.Value:X2}";
    }

    private async Task SaveConfigAsync(bool updateStatus = true)
    {
        try
        {
            Log($"SaveConfig: SelectedMonitorId='{SelectedMonitor?.Id ?? "null"}', InputSource=0x{InputSource:X2}, Rules={Rules.Count}");
            _config.Rules = Rules.ToList();
            if (SelectedMonitor is not null && !string.IsNullOrWhiteSpace(SelectedMonitor.Id))
                _config.LastSelectedMonitorId = SelectedMonitor.Id;
            if (InputSource != 0)
                _config.LastInputSource = InputSource;
            await _configStore.SaveAsync(_config);

            if (updateStatus)
                Status = string.Format(CultureInfo.CurrentUICulture, Resources.Msg_Status_Saved, _configStore.ConfigPath);

            Log("SaveConfig: done");
        }
        catch (Exception ex)
        {
            Status = string.Format(CultureInfo.CurrentUICulture, Resources.Msg_Error_Prefix, ex.Message);
            Log($"SaveConfig failed: {ex}");
        }
    }

    [RelayCommand(CanExecute = nameof(CanAddRuleFromSelection))]
    private async Task AddRuleFromSelectionAsync()
    {
        Log($"AddRule clicked: SelectedUsb={(SelectedUsbDevice is null ? "null" : "set")}, SelectedMon={(SelectedMonitor is null ? "null" : "set")}, InputSource=0x{InputSource:X2}");

        if (SelectedUsbDevice is null || SelectedMonitor is null)
        {
            Log("AddRule aborted: SelectedUsbDevice or SelectedMonitor is null");
            return;
        }

        if (string.IsNullOrWhiteSpace(SelectedMonitor.Id))
        {
            Status = Resources.Msg_Error_NoValidMonitorSelected;
            Log($"AddRule aborted: invalid monitor id ''{SelectedMonitor.Id}''");
            return;
        }

        if (string.IsNullOrWhiteSpace(SelectedUsbDevice.Vid) || string.IsNullOrWhiteSpace(SelectedUsbDevice.Pid))
        {
            Status = Resources.Msg_Error_SelectedUsbMissingVidPid;
            Log($"AddRule aborted: VID/PID missing. DeviceId='{SelectedUsbDevice.DeviceId}'");
            return;
        }

        var addedCode = OnAddedInputSourceOption?.Code ?? InputSource;
        var removedCode = OnRemovedInputSourceOption?.Code ?? (ushort)0;

        if (addedCode == 0)
        {
            Status = Resources.Msg_Error_InputSourceZero;
            Log("AddRule aborted: InputSource is 0");
            return;
         }

         var existing = Rules.FirstOrDefault(r => r.Matches(SelectedUsbDevice.Vid, SelectedUsbDevice.Pid));
         if (existing is null)
         {
             existing = new UsbTriggerRule
             {
                 Vid = SelectedUsbDevice.Vid,
                 Pid = SelectedUsbDevice.Pid,
                 OnAdded = new UsbEventAction { MonitorId = SelectedMonitor.Id, InputSource = addedCode },
                 OnRemoved = removedCode == 0 ? null : new UsbEventAction { MonitorId = SelectedMonitor.Id, InputSource = removedCode },
             };
             Rules.Add(existing);
         }
         else
         {
            existing.OnAdded ??= new UsbEventAction();
            existing.OnAdded.MonitorId = SelectedMonitor.Id;
            existing.OnAdded.InputSource = addedCode;

            if (removedCode != 0)
            {
                existing.OnRemoved ??= new UsbEventAction();
                existing.OnRemoved.MonitorId = SelectedMonitor.Id;
                existing.OnRemoved.InputSource = removedCode;
            }
            else
            {
                existing.OnRemoved = null;
            }
         }

         OnPropertyChanged(nameof(RulesDisplay));
         await SaveConfigAsync();
         Status = string.Format(CultureInfo.CurrentUICulture, Resources.Msg_Status_RuleSet, SelectedUsbDevice.Vid, SelectedUsbDevice.Pid);
         Log("AddRule: done");
     }

    private bool CanAddRuleFromSelection() => SelectedUsbDevice is not null && SelectedMonitor is not null;

    [RelayCommand]
    private async Task DeleteRuleAsync(UsbTriggerRule rule)
    {
        if (Rules.Remove(rule))
        {
            OnPropertyChanged(nameof(RulesDisplay));
            await SaveConfigAsync();
            Status = string.Format(CultureInfo.CurrentUICulture, Resources.Msg_Status_RuleDeleted, rule.Vid, rule.Pid);
            Log($"Rule deleted: VID={rule.Vid} PID={rule.Pid}");
        }
    }

    [RelayCommand]
    private void OpenConfigFolder()
    {
        try
        {
            var dir = System.IO.Path.GetDirectoryName(_configStore.ConfigPath);
            if (string.IsNullOrWhiteSpace(dir))
            {
                Status = Resources.Msg_Error_InvalidConfigDirectory;
                return;
            }

            if (!System.IO.Directory.Exists(dir))
            {
                Status = Resources.Msg_Error_ConfigDirectoryMissing;
                return;
            }

            if (OperatingSystem.IsWindows())
            {
                Process.Start("explorer.exe", dir);
            }
            else if (OperatingSystem.IsLinux())
            {
                Process.Start("xdg-open", dir);
            }
            else if (OperatingSystem.IsMacOS())
            {
                Process.Start("open", dir);
            }
            
            Status = string.Format(CultureInfo.CurrentUICulture, Resources.Msg_Status_Opened, dir);
        }
        catch (Exception ex)
        {
            Status = string.Format(CultureInfo.CurrentUICulture, Resources.Msg_Error_OpenFolderFailed, ex.Message);
        }
    }

    [RelayCommand]
    private async Task RefreshMonitorsAsync()
    {
         try
         {
             Status = Resources.Msg_Status_RefreshingMonitors;
             Monitors.Clear();
             var monitors = await _monitorController.GetMonitorsAsync();
             foreach (var m in monitors)
                 Monitors.Add(m);
             SelectedMonitor ??= Monitors.Count > 0 ? Monitors[0] : null;
             Status = string.Format(CultureInfo.CurrentUICulture, Resources.Msg_Status_FoundMonitors, Monitors.Count);
             OnPropertyChanged(nameof(RulesDisplay));
             Log($"RefreshMonitors: {Monitors.Count} monitor(s)");
             if (Monitors.Count > 0)
                 Log($"First monitor: Id=''Monitors[0].Id'', Name=''Monitors[0].Name''");
         }
         catch (Exception ex)
         {
             Status = string.Format(CultureInfo.CurrentUICulture, Resources.Msg_Error_Prefix, ex.Message);
             Log($"RefreshMonitors failed: {ex}");
         }
     }

    [RelayCommand(CanExecute = nameof(CanSwitchInput))]
    private async Task SwitchInputAsync()
    {
        if (SelectedMonitor is null)
            return;

        try
        {
            var name = SelectedInputSourceOption?.Name ?? $"0x{InputSource:X2}";
            Status = string.Format(CultureInfo.CurrentUICulture, Resources.Msg_Status_TestSwitchingTo, name, InputSource);
            await _monitorController.SetInputSourceAsync(SelectedMonitor.Id, InputSource);
            Status = Resources.Msg_Status_CommandSent;
        }
        catch (Exception ex)
        {
            Status = string.Format(CultureInfo.CurrentUICulture, Resources.Msg_Error_SwitchInputFailed, ex.Message);
        }
    }

    private bool CanSwitchInput() => SelectedMonitor is not null;

    [RelayCommand]
    private async Task TestInputSourceAsync(InputSourceOption? option)
    {
        if (SelectedMonitor is null)
        {
            Status = Resources.Msg_Error_SelectMonitorFirst;
            return;
        }

        if (option is null)
        {
            Status = Resources.Msg_Error_InvalidInputSourceOption;
            return;
        }

        try
        {
            Status = string.Format(CultureInfo.CurrentUICulture, Resources.Msg_Status_TestSwitchingTo, option.Name, option.Code);
            await _monitorController.SetInputSourceAsync(SelectedMonitor.Id, option.Code);
            Status = Resources.Msg_Status_CommandSent;
        }
        catch (Exception ex)
        {
            Status = string.Format(CultureInfo.CurrentUICulture, Resources.Msg_Error_Prefix, ex.Message);
        }
    }

    private async Task ApplyRulesAsync(UsbDeviceChangeType changeType, UsbDeviceInfo device)
    {
        if (Rules.Count == 0)
            return;

        if (string.IsNullOrWhiteSpace(device.Vid) || string.IsNullOrWhiteSpace(device.Pid))
            return;

        var matching = Rules.FirstOrDefault(r => r.Matches(device.Vid, device.Pid));
        if (matching is null)
             return;

        var action = matching.GetActionFor(changeType);
        if (action is null)
             return;

        if (string.IsNullOrWhiteSpace(action.MonitorId))
             return;

         try
         {
            Status = string.Format(
                CultureInfo.CurrentUICulture,
                Resources.Msg_Status_RuleMatched,
                device.Vid,
                device.Pid,
                changeType,
                action.InputSource);
            await _monitorController.SetInputSourceAsync(action.MonitorId!, action.InputSource);
            Status = Resources.Msg_Status_RuleApplied;
         }
         catch (Exception ex)
         {
             Status = string.Format(CultureInfo.CurrentUICulture, Resources.Msg_Error_SwitchInputFailed, ex.Message);
         }
     }

    private string GetDebugLogText() => string.Join(Environment.NewLine, DebugLog.Reverse());
    
    [RelayCommand]
    private async Task CopyDebugLogAsync()
    {
        try
        {
            var text = GetDebugLogText();
            var clipboard = PortPilot_Project.Views.MainWindow.Current?.Clipboard;
            if (clipboard is null)
            {
                Status = Resources.Msg_Error_ClipboardNotAvailable;
                return;
            }

            await Dispatcher.UIThread.InvokeAsync(async () => await clipboard.SetTextAsync(text));
            Status = Resources.Msg_Status_DebugLogCopied;
        }
        catch (Exception ex)
        {
            Status = string.Format(CultureInfo.CurrentUICulture, Resources.Msg_Error_Prefix, ex.Message);
        }
    }

    [RelayCommand]
    private async Task SaveDebugLogAsync()
    {
        try
        {
            var dir = System.IO.Path.GetDirectoryName(_configStore.ConfigPath);
            if (string.IsNullOrWhiteSpace(dir))
                dir = System.IO.Directory.GetCurrentDirectory();

            var path = System.IO.Path.Combine(dir, "debug-log.txt");
            await System.IO.File.WriteAllTextAsync(path, GetDebugLogText());
            Status = string.Format(CultureInfo.CurrentUICulture, Resources.Msg_Status_DebugLogSaved, path);
        }
        catch (Exception ex)
        {
            Status = string.Format(CultureInfo.CurrentUICulture, Resources.Msg_Error_Prefix, ex.Message);
        }
    }

    [RelayCommand]
    private async Task OpenSettingsAsync()
    {
        var owner = MainWindow.Current;
        var win = new SettingsWindow(_configStore);

        if (owner is not null)
            await win.ShowDialog(owner);
        else
            win.Show();
    }

    private void DiffUpdateUsbTargets(UsbDeviceInfo? latest)
    {
        if (latest is not null)
        {
            _recentUsbForTargets.AddFirst(latest);
            while (_recentUsbForTargets.Count > RecentUsbForTargetsLimit)
                _recentUsbForTargets.RemoveLast();
        }

        // Build a desired set of VID/PID -> representative device.
        var desired = new List<UsbDeviceInfo>();
        foreach (var d in _recentUsbForTargets)
        {
            if (!ShowAllUsbEvents)
            {
                if (string.IsNullOrWhiteSpace(d.DeviceId) || !d.DeviceId.StartsWith("USB\\", StringComparison.OrdinalIgnoreCase))
                    continue;
            }

            if (string.IsNullOrWhiteSpace(d.Vid) || string.IsNullOrWhiteSpace(d.Pid))
                continue;

            if (desired.Any(x => string.Equals(x.Vid, d.Vid, StringComparison.OrdinalIgnoreCase)
                             && string.Equals(x.Pid, d.Pid, StringComparison.OrdinalIgnoreCase)))
                continue;

            desired.Add(d);
        }

        // Diff update: remove missing
        for (var i = UsbTargets.Count - 1; i >= 0; i--)
        {
            var cur = UsbTargets[i];
            if (!desired.Any(d => string.Equals(d.Vid, cur.Vid, StringComparison.OrdinalIgnoreCase)
                               && string.Equals(d.Pid, cur.Pid, StringComparison.OrdinalIgnoreCase)))
            {
                UsbTargets.RemoveAt(i);
            }
        }

        // Add new
        foreach (var d in desired)
        {
            if (UsbTargets.Any(x => string.Equals(x.Vid, d.Vid, StringComparison.OrdinalIgnoreCase)
                                 && string.Equals(x.Pid, d.Pid, StringComparison.OrdinalIgnoreCase)))
                continue;
            UsbTargets.Add(d);
        }

        if (SelectedUsbDevice is null || string.IsNullOrWhiteSpace(SelectedUsbDevice.Vid) || string.IsNullOrWhiteSpace(SelectedUsbDevice.Pid))
            SelectedUsbDevice = UsbTargets.FirstOrDefault();
    }

    private bool _isExiting;

    public bool MinimizeToTrayOnClose
    {
        get => _config.MinimizeToTrayOnClose;
        set
        {
            if (_config.MinimizeToTrayOnClose == value)
                return;
            _config.MinimizeToTrayOnClose = value;
            OnPropertyChanged(nameof(MinimizeToTrayOnClose));
            _ = SaveConfigAsync(updateStatus: false);
        }
    }

    public void MarkExiting() => _isExiting = true;
    public bool IsExiting => _isExiting;

    public void RequestShowWindow()
    {
        if (TrayAccess.ShowWindow is { } show)
            show();
    }

    [RelayCommand]
    private void ShowWindow()
    {
        RequestShowWindow();
    }

    [RelayCommand]
    private void ToggleMonitoringActive()
    {
        IsMonitoringEnabled = !IsMonitoringEnabled;
    }

    [RelayCommand]
    private async Task ExitApplicationAsync()
    {
        MarkExiting();
        await SaveConfigAsync(updateStatus: false);
        if (TrayAccess.ExitApplication is { } exit)
            exit();
    }

    public static class TrayAccess
    {
        public static Action? ShowWindow { get; set; }
        public static Action? ExitApplication { get; set; }
    }

    private sealed class NullMonitorController : IMonitorController
    {
        public Task<IReadOnlyList<MonitorInfo>> GetMonitorsAsync(System.Threading.CancellationToken cancellationToken = default)
            => Task.FromResult<IReadOnlyList<MonitorInfo>>(Array.Empty<MonitorInfo>());

        public Task SetInputSourceAsync(string monitorId, ushort sourceCode, System.Threading.CancellationToken cancellationToken = default)
            => Task.CompletedTask;
    }

    private sealed class NullUsbWatcher : IUsbWatcher
    {
        public event EventHandler<UsbDeviceChangedEventArgs>? DeviceChanged
        {
            add { }
            remove { }
        }
        public void Start() { }
        public void Stop() { }
        public List<UsbDeviceInfo> GetConnectedDevices() => new();
        public void Dispose() { }
    }
}
