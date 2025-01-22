using System.Threading.Tasks;
using System.Threading;

namespace Sbw.SimpleLoops;

public interface ISimpleLoop
{
    Task RunAsync(CancellationToken stoppingToken);
}
