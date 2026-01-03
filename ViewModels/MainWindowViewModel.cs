using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using PortPilot_Project.Abstractions;
using PortPilot_Project.Config;
using PortPilot_Project.Models;
using PortPilot_Project.Windows;

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
        new InputSourceOption("DisplayPort 1", 0x0F),
        new InputSourceOption("DisplayPort 2", 0x10),
        new InputSourceOption("HDMI 1", 0x11),
        new InputSourceOption("HDMI 2", 0x12),
        new InputSourceOption("D-Sub (VGA)", 0x01),
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
        _monitorController = OperatingSystem.IsWindows() ? new WinMonitorController() : new NullMonitorController();
        _usbWatcher = OperatingSystem.IsWindows() ? new WinUsbWatcher() : new NullUsbWatcher();
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
             // WMI events come from a non-UI thread.
             Dispatcher.UIThread.Post(async () =>
             {
                 Log($"USB {e.ChangeType} Name='{e.Device.Name}' Vid={e.Device.Vid ?? "null"} Pid={e.Device.Pid ?? "null"}");
                 Log($"USB DeviceId='{e.Device.DeviceId}'");

                 // Only keep the raw event list in debug mode.
                 if (IsDebugMode)
                     RecentUsbEvents.Insert(0, e.Device);

                 // Update targets immediately (no debounce).
                 DiffUpdateUsbTargets(e.Device);

                 Status = $"USB {e.ChangeType}: {e.Device.Name ?? e.Device.DeviceId}";

                 await ApplyRulesAsync(e.ChangeType, e.Device);
             });
         };

         try
         {
             _usbWatcher.Start();
            Log("USB watcher started");
         }
         catch (Exception ex)
         {
             Status = ex.Message;
            Log($"USB watcher start failed: {ex}");
         }

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

        Log($"InitializeAsync done: Monitors={Monitors.Count}, SelectedMonitorId='{SelectedMonitor?.Id ?? "null"}', InputSource=0x{InputSource:X2}");
     }

    partial void OnOnAddedInputSourceOptionChanged(InputSourceOption? value)
    {
        if (value is null)
            return;

        // Keep the main InputSource in sync with the 'Added' action for convenience.
        InputSource = value.Code;
    }

    partial void OnSelectedInputSourceOptionChanged(InputSourceOption? value)
    {
        if (value is null)
            return;
        InputSource = value.Code;
    }

    partial void OnInputSourceChanged(ushort value)
    {
        SelectedInputSourceOption = InputSourceOptions.FirstOrDefault(o => o.Code == value) ?? SelectedInputSourceOption;
    }

    [RelayCommand]
    private async Task LoadConfigAsync()
    {
        try
        {
            _config = await _configStore.LoadAsync();
            Rules.Clear();
            foreach (var r in _config.Rules)
                Rules.Add(r);

            OnPropertyChanged(nameof(RulesDisplay));
        }
        catch (Exception ex)
        {
            Status = ex.Message;
            _config = new AppConfig();
            Rules.Clear();

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

    [RelayCommand]
    private async Task SaveConfigAsync()
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
            Status = $"Saved: {_configStore.ConfigPath}";
             Log("SaveConfig: done");
         }
         catch (Exception ex)
         {
            Status = ex.Message;
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
            Status = "No valid monitor selected (DDC/CI monitor not detected).";
            Log($"AddRule aborted: invalid monitor id '{SelectedMonitor.Id}'");
            return;
        }

        if (string.IsNullOrWhiteSpace(SelectedUsbDevice.Vid) || string.IsNullOrWhiteSpace(SelectedUsbDevice.Pid))
        {
            Status = "Selected USB device does not have VID/PID.";
            Log($"AddRule aborted: VID/PID missing. DeviceId='{SelectedUsbDevice.DeviceId}'");
            return;
        }

        var addedCode = OnAddedInputSourceOption?.Code ?? InputSource;
        var removedCode = OnRemovedInputSourceOption?.Code ?? (ushort)0;

        if (addedCode == 0)
        {
            Status = "InputSource is 0. Please choose a valid input source.";
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
         Status = $"Rule set for VID:{SelectedUsbDevice.Vid} PID:{SelectedUsbDevice.Pid}";
         Log("AddRule: done");
     }

    private bool CanAddRuleFromSelection() => SelectedUsbDevice is not null && SelectedMonitor is not null;

    [RelayCommand]
    private async Task RefreshMonitorsAsync()
    {
         try
         {
             Status = "Refreshing monitors...";
             Monitors.Clear();
             var monitors = await _monitorController.GetMonitorsAsync();
             foreach (var m in monitors)
                 Monitors.Add(m);
             SelectedMonitor ??= Monitors.Count > 0 ? Monitors[0] : null;
             Status = $"Found {Monitors.Count} monitor(s).";
             OnPropertyChanged(nameof(RulesDisplay));
             Log($"RefreshMonitors: {Monitors.Count} monitor(s)");
             if (Monitors.Count > 0)
                 Log($"First monitor: Id='{Monitors[0].Id}', Name='{Monitors[0].Name}'");
         }
         catch (Exception ex)
         {
             Status = ex.Message;
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
            Status = $"Switching input: 0x{InputSource:X2}";
            await _monitorController.SetInputSourceAsync(SelectedMonitor.Id, InputSource);
            Status = "Input switched.";
        }
        catch (Exception ex)
        {
            Status = ex.Message;
        }
    }

    private bool CanSwitchInput() => SelectedMonitor is not null;

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
            Status = $"Rule matched VID:{device.Vid} PID:{device.Pid} ({changeType}) -> input 0x{action.InputSource:X2}";
            await _monitorController.SetInputSourceAsync(action.MonitorId!, action.InputSource);
            Status = "Rule applied.";
         }
         catch (Exception ex)
         {
             Status = ex.Message;
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
                Status = "Clipboard not available.";
                return;
            }

            await Dispatcher.UIThread.InvokeAsync(async () => await clipboard.SetTextAsync(text));
            Status = "Debug log copied to clipboard.";
        }
        catch (Exception ex)
        {
            Status = ex.Message;
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
            Status = $"Debug log saved: {path}";
        }
        catch (Exception ex)
        {
            Status = ex.Message;
        }
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
        public void Dispose() { }
    }
}
