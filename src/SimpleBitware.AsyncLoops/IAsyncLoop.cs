using System.Threading.Tasks;
using System.Threading;

namespace SimpleBitware.AsyncLoops;

/// <summary>
/// Simple loop interface.
/// </summary>
public interface IAsyncLoop
{
    Task RunAsync(CancellationToken stoppingToken);
}
