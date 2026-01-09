using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Moq;
using SimpleBitware.AsyncLoops;
using SimpleBitware.Common.Abstractions;

namespace Sb.SimpleLoops.Tests.End2End;

public class AsyncLoopsBackgroundServiceTests
{
    [Test]
    public async Task Should_Run_Loops_And_Exit_When_Loop_Completed()
    {
        // Arrange
        var loopIteratorExecutorMock = new Mock<IAsyncLoopIterationExecutor>();
        loopIteratorExecutorMock.Setup(x => x.RunAsync(It.IsAny<CancellationToken>()))
            .Returns<CancellationToken>(cancellationToken => Task.Run(async () =>
            {
                await Task.Delay(1000, cancellationToken);
                return IterationResult.Stop;
            }, cancellationToken));

        var builder = Host.CreateDefaultBuilder()
            .ConfigureServices(services =>
            {
                services.AddHostedService<AsyncLoopsBackgroundService>();
                services.AddSingleton<IAsyncLoop, AsyncLoop<IAsyncLoopIterationExecutor>>();
                services.AddSingleton<IAsyncLoopIterationExecutor>(services => loopIteratorExecutorMock.Object);
                services.AddSingleton<AsyncLoopConfiguration<IAsyncLoopIterationExecutor>>();
                services.AddSingleton<ITask, TaskProvider>();
                services.AddSingleton<IDateTime, DateTimeProvider>();
            });
        using var host = builder.Build();

        var cancellationTokenSource = new CancellationTokenSource();

        // Act
        await host.RunAsync(cancellationTokenSource.Token);

        // Assert
        loopIteratorExecutorMock.Verify(x => x.RunAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Test]
    public async Task Should_Run_Loops_And_Exist_When_Cancellation_Token_Cancelled()
    {
        // Arrange
        var loopIteratorExecutorMock = new Mock<IAsyncLoopIterationExecutor>();
        loopIteratorExecutorMock.Setup(x => x.RunAsync(It.IsAny<CancellationToken>()))
            .Returns<CancellationToken>(cancellationToken => Task.Run(async () =>
            {
                do
                {
                    await Task.Delay(10, cancellationToken);
                } while (!cancellationToken.IsCancellationRequested);

                return IterationResult.Wait;
            }, cancellationToken));

        var builder = Host.CreateDefaultBuilder()
            .ConfigureServices(services =>
            {
                services.AddHostedService<AsyncLoopsBackgroundService>();
                services.AddSingleton<IAsyncLoop, AsyncLoop<IAsyncLoopIterationExecutor>>();
                services.AddSingleton(services => loopIteratorExecutorMock.Object);
                services.AddSingleton<ITask, TaskProvider>();
                services.AddSingleton<IDateTime, DateTimeProvider>();
            });
        using var host = builder.Build();

        var cancellationTokenSource = new CancellationTokenSource();

        // Act
        var task = host.RunAsync(cancellationTokenSource.Token);
        await Task.Delay(100);
        cancellationTokenSource.Cancel();
        await task;

        // Assert
        loopIteratorExecutorMock.Verify(x => x.RunAsync(It.IsAny<CancellationToken>()), Times.AtLeastOnce);
    }
}
