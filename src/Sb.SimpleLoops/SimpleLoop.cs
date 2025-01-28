using System.Threading.Tasks;
using System.Threading;
using System;
using Microsoft.Extensions.Logging;
using System.Linq;
using Sb.Common.Wrappers;

namespace Sb.SimpleLoops;

/// <summary>
/// Simple loop which invokes iterator executor.
/// </summary>
/// <typeparam name="T">The type of iterator executor.</typeparam>
public class SimpleLoop<T> : ISimpleLoop
    where T : ISimpleLoopIterationExecutor
{
    private readonly ILogger<SimpleLoop<T>> logger;
    private readonly SimpleLoopConfiguration<T> configuration;
    private readonly T iterationExecutor;
    private readonly ITaskDelayWrapper taskDelayWrapper;
    private readonly IDateTimeWrapper dateTimeWrapper;

    public SimpleLoop(
        ILogger<SimpleLoop<T>> logger,
        SimpleLoopConfiguration<T> configuration,
        T iterationExecutor,
        ITaskDelayWrapper taskDelayWrapper,
        IDateTimeWrapper dateTimeWrapper)
    {
        this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
        this.configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
        this.iterationExecutor = iterationExecutor ?? throw new ArgumentNullException(nameof(iterationExecutor));
        this.taskDelayWrapper = taskDelayWrapper ?? throw new ArgumentNullException(nameof(taskDelayWrapper));
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
                    await taskDelayWrapper.Delay(configuration.WaitingTimeInMs, cancellationToken);
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
