using PortPilot_Project.Abstractions;

namespace PortPilot.Tests.TestDoubles;

internal sealed class FakeMonitorController : IMonitorController
{
    private readonly TaskCompletionSource _release = new(TaskCreationOptions.RunContinuationsAsynchronously);
    private readonly bool _delayOperations;
    private readonly Exception? _failure;
    private int _concurrentCalls;
    private int _maxConcurrentCalls;

    internal FakeMonitorController(bool delayOperations = false, Exception? failure = null)
    {
        _delayOperations = delayOperations;
        _failure = failure;
    }

    internal TaskCompletionSource GetMonitorsStarted { get; } =
        new(TaskCreationOptions.RunContinuationsAsynchronously);

    internal List<(string MonitorId, ushort SourceCode)> Calls { get; } = new();

    internal int MaxConcurrentCalls => Volatile.Read(ref _maxConcurrentCalls);

    internal void Release()
        => _release.TrySetResult();

    public async Task<IReadOnlyList<MonitorInfo>> GetMonitorsAsync(
        CancellationToken cancellationToken = default)
    {
        GetMonitorsStarted.TrySetResult();
        if (_delayOperations)
            await _release.Task.WaitAsync(cancellationToken);

        if (_failure is not null)
            throw _failure;

        return Array.Empty<MonitorInfo>();
    }

    public async Task SetInputSourceAsync(
        string monitorId,
        ushort sourceCode,
        CancellationToken cancellationToken = default)
    {
        var concurrentCalls = Interlocked.Increment(ref _concurrentCalls);
        UpdateMaximum(concurrentCalls);
        try
        {
            lock (Calls)
                Calls.Add((monitorId, sourceCode));

            if (_delayOperations)
                await _release.Task.WaitAsync(cancellationToken);

            if (_failure is not null)
                throw _failure;
        }
        finally
        {
            Interlocked.Decrement(ref _concurrentCalls);
        }
    }

    private void UpdateMaximum(int value)
    {
        var current = Volatile.Read(ref _maxConcurrentCalls);
        while (value > current)
        {
            var observed = Interlocked.CompareExchange(ref _maxConcurrentCalls, value, current);
            if (observed == current)
                return;
            current = observed;
        }
    }
}
