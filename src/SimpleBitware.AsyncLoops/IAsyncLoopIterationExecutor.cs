using System.Threading;
using System.Threading.Tasks;

namespace SimpleBitware.AsyncLoops;

/// <summary>
/// Simple loop iteration executor interface.
/// </summary>
public interface IAsyncLoopIterationExecutor
{
    /// <summary>
    /// Run a single iteration of the loop.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>False when it should wait before running the next iteration otherwise true.</returns>
    Task<IterationResult> RunAsync(CancellationToken cancellationToken);
}
