using System.Threading.Tasks;
using System.Threading;

namespace Sb.SimpleLoops;

/// <summary>
/// Simple loop interface.
/// </summary>
public interface ISimpleLoop
{
    Task RunAsync(CancellationToken stoppingToken);
}
