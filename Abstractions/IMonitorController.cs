using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace PortPilot_Project.Abstractions;

public interface IMonitorController
{
    Task<IReadOnlyList<MonitorInfo>> GetMonitorsAsync(CancellationToken cancellationToken = default);
    Task SetInputSourceAsync(string monitorId, ushort sourceCode, CancellationToken cancellationToken = default);
}
