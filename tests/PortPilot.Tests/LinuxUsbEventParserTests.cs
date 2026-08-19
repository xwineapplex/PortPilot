using PortPilot_Project.Linux;

namespace PortPilot.Tests;

public sealed class LinuxUsbEventParserTests
{
    [Fact]
    public void FixturePreservesAddAndRemoveEventProperties()
    {
        var parser = new LinuxUsbEventParser();
        var fixturePath = Path.Combine(AppContext.BaseDirectory, "Fixtures", "udevadm-monitor.txt");
        var events = new List<IReadOnlyDictionary<string, string>>();

        foreach (var line in File.ReadLines(fixturePath))
        {
            if (parser.ReadLine(line) is { } completed)
                events.Add(completed);
        }

        if (parser.Complete() is { } finalEvent)
            events.Add(finalEvent);

        Assert.Collection(
            events,
            added =>
            {
                Assert.Equal("add", added["ACTION"]);
                Assert.Equal("/devices/pci0000:00/usb1/1-2", added["DEVPATH"]);
                Assert.Equal("usb_device", added["DEVTYPE"]);
                Assert.Equal("046d", added["ID_VENDOR_ID"]);
                Assert.Equal("c534", added["ID_MODEL_ID"]);
                Assert.Equal("ABC123", added["ID_SERIAL_SHORT"]);
            },
            removed =>
            {
                Assert.Equal("remove", removed["ACTION"]);
                Assert.Equal("/devices/pci0000:00/usb1/1-2", removed["DEVPATH"]);
            });
    }

    [Fact]
    public void NewHeaderFlushesPreviousEventOnlyOnce()
    {
        var parser = new LinuxUsbEventParser();

        Assert.Null(parser.ReadLine("UDEV add"));
        Assert.Null(parser.ReadLine("ACTION=add"));
        Assert.Null(parser.ReadLine("DEVPATH=/devices/usb1/1-1"));

        var completed = parser.ReadLine("UDEV remove");

        Assert.NotNull(completed);
        Assert.Equal("add", completed["ACTION"]);
        Assert.Null(parser.ReadLine(""));
    }

    [Fact]
    public void CompleteFlushesACompleteFinalEventAndIgnoresAnIncompleteEvent()
    {
        var completeParser = new LinuxUsbEventParser();
        completeParser.ReadLine("UDEV add");
        completeParser.ReadLine("ACTION=add");
        completeParser.ReadLine("DEVPATH=/devices/usb1/1-1");

        var completed = completeParser.Complete();

        Assert.NotNull(completed);
        Assert.Equal("add", completed["ACTION"]);
        Assert.Null(completeParser.Complete());

        var incompleteParser = new LinuxUsbEventParser();
        incompleteParser.ReadLine("UDEV add");
        incompleteParser.ReadLine("ACTION=add");
        Assert.Null(incompleteParser.Complete());
    }

    [Fact]
    public void StartupBannerAndMalformedPropertiesAreIgnored()
    {
        var parser = new LinuxUsbEventParser();

        Assert.Null(parser.ReadLine("monitor will print the received events for:"));
        Assert.Null(parser.ReadLine("not-a-property"));
        Assert.Null(parser.ReadLine(""));
        Assert.Null(parser.Complete());
    }
}
