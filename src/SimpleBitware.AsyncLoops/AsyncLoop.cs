using System.Threading.Tasks;
using System.Threading;
using System;
using Microsoft.Extensions.Logging;
using System.Linq;
using SimpleBitware.Common.Abstractions;

namespace SimpleBitware.AsyncLoops;

/// <summary>
/// Simple loop which invokes iterator executor.
/// </summary>
/// <typeparam name="T">The type of iterator executor.</typeparam>
public class AsyncLoop<T> : IAsyncLoop
    where T : IAsyncLoopIterationExecutor
{
    private readonly ILogger<AsyncLoop<T>> logger;
    private readonly AsyncLoopConfiguration<T> configuration;
    private readonly T iterationExecutor;
    private readonly ITask taskWrapper;
    private readonly IDateTime dateTimeWrapper;

    public AsyncLoop(
        ILogger<AsyncLoop<T>> logger,
        T iterationExecutor,
        ITask taskWrapper,
        IDateTime dateTimeWrapper
        ): this(logger, new AsyncLoopConfiguration<T>(), iterationExecutor, taskWrapper, dateTimeWrapper)
    {
    }

    public AsyncLoop(
        ILogger<AsyncLoop<T>> logger,
        AsyncLoopConfiguration<T> configuration,
        T iterationExecutor,
        ITask taskWrapper,
        IDateTime dateTimeWrapper)
    {
        this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
        this.configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
        this.iterationExecutor = iterationExecutor ?? throw new ArgumentNullException(nameof(iterationExecutor));
        this.taskWrapper = taskWrapper ?? throw new ArgumentNullException(nameof(taskWrapper));
        this.dateTimeWrapper = dateTimeWrapper ?? throw new ArgumentNullException(nameof(dateTimeWrapper));
    }

    public async Task RunAsync(CancellationToken cancellationToken)
    {
        logger.LogInformation("Loop started");

        while (!cancellationToken.IsCancellationRequested)
        {
            var iterationContinuation = IterationResult.Wait;

            try
            {
                logger.LogInformation("Iteration started");
                iterationContinuation = await iterationExecutor.RunAsync(cancellationToken);
            }
            catch (OperationCanceledException ex)
            {
                logger.LogWarning(ex, "Loop cancelled.");
                return;
            }
            catch (AggregateException ae)
            {
                ae.Flatten().InnerExceptions
                    .ToList()
                    .ForEach(x =>
                    {
                        HandleException(x);
                    });
            }
            catch (Exception ex)
            {
                HandleException(ex);
            }

            switch (iterationContinuation)
            {
                case IterationResult.Continue:
                    logger.LogInformation("Iteration completed.");
                    break;
                case IterationResult.Stop:
                    logger.LogInformation("Iteration completed and loop stopped.");
                    return;
                case IterationResult.Wait:
                    logger.LogInformation("Iteration completed. Next run at {nextRun}", dateTimeWrapper.UtcNow.AddMilliseconds(configuration.WaitingTimeInMs));
                    await taskWrapper.Delay(configuration.WaitingTimeInMs, cancellationToken);
                    break;
            }
        }
    }

    protected virtual void HandleException(Exception ex)
    {
        logger.LogError(ex, "Unexpected exception.");
        if ((ex is StackOverflowException or OutOfMemoryException) || configuration.PropagateExceptions)
            throw ex;
    }
}
