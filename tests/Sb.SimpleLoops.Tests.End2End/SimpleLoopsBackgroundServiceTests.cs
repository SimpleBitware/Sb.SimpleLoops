using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Moq;
using Sb.Common.Wrappers;

namespace Sb.SimpleLoops.Tests.End2End
{
    public class SimpleLoopsBackgroundServiceTests
    {
        [Test]
        public async Task Should_Run_Loops_And_Exit_When_Loop_Completed()
        {
            // Arrange
            var loopIteratorExecutorMock = new Mock<ISimpleLoopIterationExecutor>();
            loopIteratorExecutorMock.Setup(x => x.RunAsync(It.IsAny<CancellationToken>()))
                .Returns<CancellationToken>(cancellationToken => Task.Run(async () =>
                {
                    await Task.Delay(1000, cancellationToken);
                    return IterationResult.Stop;
                }, cancellationToken));

            var builder = Host.CreateDefaultBuilder()
                .ConfigureServices(services =>
                {
                    services.AddHostedService<SimpleLoopsBackgroundService>();
                    services.AddScoped<ISimpleLoop, SimpleLoop<ISimpleLoopIterationExecutor>>();
                    services.AddScoped<ISimpleLoopIterationExecutor>(services => loopIteratorExecutorMock.Object);
                    services.AddSingleton<SimpleLoopConfiguration<ISimpleLoopIterationExecutor>>();
                    services.AddSingleton<ITask, TaskWrapper>();
                    services.AddSingleton<IDateTime, DateTimeWrapper>();
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
            var loopIteratorExecutorMock = new Mock<ISimpleLoopIterationExecutor>();
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
                    services.AddHostedService<SimpleLoopsBackgroundService>();
                    services.AddSingleton<ISimpleLoop, SimpleLoop<ISimpleLoopIterationExecutor>>();
                    services.AddSingleton(services => loopIteratorExecutorMock.Object);
                    services.AddSingleton<ITask, TaskWrapper>();
                    services.AddSingleton<IDateTime, DateTimeWrapper>();
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
}