using PortPilot_Project.Linux;

namespace PortPilot.Tests.TestDoubles;

internal sealed class FakeUdevMonitorProcess : IUdevMonitorProcess
{
    internal FakeUdevMonitorProcess(string standardOutput = "", string standardError = "")
    {
        StandardOutput = new StringReader(standardOutput);
        StandardError = new StringReader(standardError);
    }

    public TextReader StandardOutput { get; }

    public TextReader StandardError { get; }

    public bool HasExited { get; private set; }

    internal int KillCount { get; private set; }

    internal int WaitForExitCount { get; private set; }

    internal int DisposeCount { get; private set; }

    internal void MarkExited()
        => HasExited = true;

    public void Kill(bool entireProcessTree)
    {
        Assert.True(entireProcessTree);
        KillCount++;
        HasExited = true;
    }

    public bool WaitForExit(TimeSpan timeout)
    {
        WaitForExitCount++;
        HasExited = true;
        return true;
    }

    public void Dispose()
        => DisposeCount++;
}
