using PortPilot_Project.Config;
using PortPilot.Tests.Support;

namespace PortPilot.Tests;

public sealed class ConfigStoreBaselineTests
{
    [Fact]
    public async Task TemporaryConfigCanBeSavedAndLoaded()
    {
        using var directory = new TemporaryDirectory();
        var store = new ConfigStore(directory.GetPath("config.json"));
        var expected = new AppConfig { Language = "zh-Hant", MonitoringEnabled = false };

        await store.SaveAsync(expected);
        var actual = await store.LoadAsync();

        Assert.Equal("zh-Hant", actual.Language);
        Assert.False(actual.MonitoringEnabled);
    }
}
