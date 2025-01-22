using System.Threading.Tasks;
using System.Threading;
using System;
using Microsoft.Extensions.Logging;
using System.Linq;
using Sbw.Wrappers;

namespace Sbw.SimpleLoops;

public class SimpleLoop: ISimpleLoop
{
    private readonly ILogger<SimpleLoop> logger;
    private readonly SimpleLoopConfiguration configuration;
    private readonly ISimpleLoopIterationExecutor iterationExecutor;
    private readonly ITaskDelayWrapper taskDelayWrapper;

    private readonly string loopDescriptor;

    public SimpleLoop(
        ILogger<SimpleLoop> logger,
        SimpleLoopConfiguration configuration,
        ISimpleLoopIterationExecutor iterationExecutor,
        ITaskDelayWrapper taskDelayWrapper)
    {
        this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
        this.configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
        this.iterationExecutor = iterationExecutor ?? throw new ArgumentNullException(nameof(iterationExecutor));
        this.taskDelayWrapper = taskDelayWrapper ?? throw new ArgumentNullException(nameof(taskDelayWrapper));
        this.loopDescriptor = iterationExecutor.GetType().Name;
    }

    public async Task RunAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("{loopDescriptor} loop started at: {dateTime}", loopDescriptor, DateTime.UtcNow);

        while (!stoppingToken.IsCancellationRequested)
        {
            bool continueWithoutWaiting = false;

            try
            {
                continueWithoutWaiting = await iterationExecutor.RunAsync(stoppingToken);
            }
            catch (OperationCanceledException ex)
            {
                logger.LogWarning(ex, "Cancelled.");
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

            if (!continueWithoutWaiting)
            {
                logger.LogInformation("{loopDescriptor} loop completed at: {dateTime}. Next run at {nextRun}",
                    loopDescriptor, DateTime.UtcNow, DateTime.UtcNow.AddMilliseconds(configuration.WaitingTimeInMs));

                await taskDelayWrapper.DelayAsync(configuration.WaitingTimeInMs, stoppingToken);
            }
        }

        logger.LogInformation("{loopDescriptor} loop stopped at: {dateTime}", loopDescriptor, DateTime.UtcNow);
    }

    protected virtual void HandleException(Exception ex)
    {
        logger.LogError(ex, "Unhandled exception.");
        if ((ex is StackOverflowException or OutOfMemoryException) || configuration.PropagateException)
            throw ex;
    }
}
