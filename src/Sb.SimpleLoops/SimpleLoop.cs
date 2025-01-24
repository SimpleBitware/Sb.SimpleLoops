﻿using System.Threading.Tasks;
using System.Threading;
using System;
using Microsoft.Extensions.Logging;
using System.Linq;
using Sb.Common.Wrappers;

namespace Sb.SimpleLoops;

public class SimpleLoop<T>: ISimpleLoop
    where T : ISimpleLoopIterationExecutor
{
    private readonly ILogger<SimpleLoop<T>> logger;
    private readonly SimpleLoopConfiguration configuration;
    private readonly T iterationExecutor;
    private readonly ITaskDelayWrapper taskDelayWrapper;
    private readonly IDateTimeWrapper dateTimeWrapper;
    private readonly string loopDescriptor;

    public SimpleLoop(
        ILogger<SimpleLoop<T>> logger,
        SimpleLoopConfiguration configuration,
        T iterationExecutor,
        ITaskDelayWrapper taskDelayWrapper,
        IDateTimeWrapper dateTimeWrapper)
    {
        this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
        this.configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
        this.iterationExecutor = iterationExecutor ?? throw new ArgumentNullException(nameof(iterationExecutor));
        this.taskDelayWrapper = taskDelayWrapper ?? throw new ArgumentNullException(nameof(taskDelayWrapper));
        this.dateTimeWrapper = dateTimeWrapper ?? throw new ArgumentNullException(nameof(dateTimeWrapper));
        this.loopDescriptor = iterationExecutor.GetType().Name;
    }

    public async Task RunAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("{loopDescriptor} loop started at: {dateTime}", loopDescriptor, dateTimeWrapper.UtcNow);

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
                    loopDescriptor, dateTimeWrapper.UtcNow, dateTimeWrapper.UtcNow.AddMilliseconds(configuration.WaitingTimeInMs));

                await taskDelayWrapper.DelayAsync(configuration.WaitingTimeInMs, stoppingToken);
            }
        }

        logger.LogInformation("{loopDescriptor} loop stopped at: {dateTime}", loopDescriptor, dateTimeWrapper.UtcNow);
    }

    protected virtual void HandleException(Exception ex)
    {
        logger.LogError(ex, "Unhandled exception.");
        if ((ex is StackOverflowException or OutOfMemoryException) || configuration.PropagateException)
            throw ex;
    }
}
