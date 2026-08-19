using PortPilot_Project.Abstractions;
using PortPilot_Project.Linux;
using PortPilot.Tests.TestDoubles;

namespace PortPilot.Tests;

public sealed class LinuxUsbWatcherTests
{
    [Fact]
    public void StartStopAndDisposeAreIdempotent()
    {
        var processes = new List<FakeUdevMonitorProcess>();
        using var watcher = new LinuxUsbWatcher(
            () =>
            {
                var process = new FakeUdevMonitorProcess();
                processes.Add(process);
                return process;
            },
            isSupported: true);

        watcher.Start();
        watcher.Start();
        watcher.Stop();
        watcher.Stop();
        watcher.Start();
        watcher.Dispose();
        watcher.Dispose();

        Assert.Equal(2, processes.Count);
        Assert.All(processes, process => Assert.Equal(1, process.DisposeCount));
    }

    [Fact]
    public void ProcessStartFailureDoesNotPoisonNextStart()
    {
        var attempts = 0;
        var process = new FakeUdevMonitorProcess();
        using var watcher = new LinuxUsbWatcher(
            () =>
            {
                attempts++;
                return attempts == 1
                    ? throw new InvalidOperationException("Simulated start failure.")
                    : process;
            },
            isSupported: true);

        Assert.Throws<InvalidOperationException>(() => watcher.Start());

        watcher.Start();
        watcher.Stop();

        Assert.Equal(2, attempts);
        Assert.Equal(1, process.DisposeCount);
    }

    [Fact]
    public void StopIsSafeWhenProcessAlreadyExited()
    {
        var process = new FakeUdevMonitorProcess();
        using var watcher = new LinuxUsbWatcher(() => process, isSupported: true);

        watcher.Start();
        process.MarkExited();
        watcher.Stop();

        Assert.Equal(0, process.KillCount);
        Assert.Equal(1, process.DisposeCount);
    }

    [Fact]
    public async Task BlankLineDeliversAnEventWithoutAnotherHeader()
    {
        const string output = """
            UDEV add
            ACTION=add
            DEVPATH=/devices/usb1/1-1
            DEVTYPE=usb_device
            ID_VENDOR_ID=046d
            ID_MODEL_ID=c534
            ID_SERIAL_SHORT=ABC123

            """;
        var process = new FakeUdevMonitorProcess(output);
        using var watcher = new LinuxUsbWatcher(() => process, isSupported: true);
        var received = new TaskCompletionSource<UsbDeviceChangedEventArgs>(
            TaskCreationOptions.RunContinuationsAsynchronously);
        watcher.DeviceChanged += (_, args) => received.TrySetResult(args);

        watcher.Start();
        var eventArgs = await received.Task.WaitAsync(TimeSpan.FromSeconds(2));

        Assert.Equal(UsbDeviceChangeType.Added, eventArgs.ChangeType);
        Assert.Equal("046D", eventArgs.Device.Vid);
        Assert.Equal("C534", eventArgs.Device.Pid);
        Assert.Contains("ABC123", eventArgs.Device.DeviceId);
    }

    [Fact]
    public async Task AddAndRemovePreserveDeviceIdentity()
    {
        const string output = """
            UDEV add
            ACTION=add
            DEVPATH=/devices/usb1/1-1
            DEVTYPE=usb_device
            ID_VENDOR_ID=046d
            ID_MODEL_ID=c534
            ID_SERIAL_SHORT=ABC123

            UDEV remove
            ACTION=remove
            DEVPATH=/devices/usb1/1-1
            DEVTYPE=usb_device

            """;
        var process = new FakeUdevMonitorProcess(output);
        using var watcher = new LinuxUsbWatcher(() => process, isSupported: true);
        var events = new List<UsbDeviceChangedEventArgs>();
        watcher.DeviceChanged += (_, args) => events.Add(args);

        watcher.Start();
        await watcher.ReadCompletion.WaitAsync(TimeSpan.FromSeconds(2));

        Assert.Collection(
            events,
            added => Assert.Equal(UsbDeviceChangeType.Added, added.ChangeType),
            removed =>
            {
                Assert.Equal(UsbDeviceChangeType.Removed, removed.ChangeType);
                Assert.Equal(events[0].Device, removed.Device);
            });
    }

    [Fact]
    public async Task UsbInterfaceEventsAreIgnored()
    {
        const string output = """
            UDEV add
            ACTION=add
            DEVPATH=/devices/usb1/1-1/1-1:1.0
            DEVTYPE=usb_interface
            ID_VENDOR_ID=046d
            ID_MODEL_ID=c534

            """;
        var process = new FakeUdevMonitorProcess(output);
        using var watcher = new LinuxUsbWatcher(() => process, isSupported: true);
        var eventCount = 0;
        watcher.DeviceChanged += (_, _) => eventCount++;

        watcher.Start();
        await watcher.ReadCompletion.WaitAsync(TimeSpan.FromSeconds(2));

        Assert.Equal(0, eventCount);
    }
}
