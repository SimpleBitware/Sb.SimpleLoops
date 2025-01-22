using System.Threading.Tasks;
using System.Threading;

namespace Sb.SimpleLoops;

public interface ISimpleLoop
{
    Task RunAsync(CancellationToken stoppingToken);
}
