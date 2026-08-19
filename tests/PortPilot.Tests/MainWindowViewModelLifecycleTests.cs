using PortPilot_Project.Config;
using PortPilot_Project.ViewModels;
using PortPilot.Tests.Support;
using PortPilot.Tests.TestDoubles;

namespace PortPilot.Tests;

public sealed class MainWindowViewModelLifecycleTests
{
    [Fact]
    public async Task ShutdownDuringInitializationDoesNotStartAndDisposesWatcherOnce()
    {
        using var directory = new TemporaryDirectory();
        var configStore = new ConfigStore(directory.GetPath("config.json"));
        await configStore.SaveAsync(new AppConfig { MonitoringEnabled = true });
        var monitorController = new FakeMonitorController(delayOperations: true);
        var watcher = new FakeUsbWatcher();
        var viewModel = new MainWindowViewModel(monitorController, watcher, configStore);
        await monitorController.GetMonitorsStarted.Task.WaitAsync(TimeSpan.FromSeconds(2));

        viewModel.Dispose();
        viewModel.Dispose();
        monitorController.Release();
        await viewModel.InitializationTask.WaitAsync(TimeSpan.FromSeconds(2));

        Assert.Equal(0, watcher.StartCount);
        Assert.Equal(1, watcher.StopCount);
        Assert.Equal(1, watcher.DisposeCount);
    }
}
