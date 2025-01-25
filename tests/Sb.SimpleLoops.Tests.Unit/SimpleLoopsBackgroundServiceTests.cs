using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Moq;

namespace Sb.SimpleLoops.Tests.Unit;

public class SimpleLoopsBackgroundServiceTests
{
    private Mock<ILogger<SimpleLoopsBackgroundService>> loggerMock;

    [OneTimeSetUp]
    public void OneTimeSetUp()
    {
        loggerMock = new Mock<ILogger<SimpleLoopsBackgroundService>>();
    }

    [Test]
    public async Task Should_Stop_When_No_Loops()
    {
        // Arrange
        var hostApplicationLifetimeMock = new Mock<IHostApplicationLifetime>();
        var serviceCollection = new ServiceCollection();
        var serviceProvider = serviceCollection.BuildServiceProvider();
        var sut = new SimpleLoopsBackgroundService(
            serviceProvider,
            hostApplicationLifetimeMock.Object,
            loggerMock.Object);

        // Act

        await sut.StartAsync(CancellationToken.None);

        // Assert
        hostApplicationLifetimeMock.Verify(x => x.StopApplication(), Times.Never);
    }

    [Test]
    public async Task Should_Stop_When_Loop_Cancelled()
    {
        // Arrange
        var simpleLoopMock = new Mock<ISimpleLoop>();
        simpleLoopMock.Setup(x => x.RunAsync(It.IsAny<CancellationToken>()))
            .Returns<CancellationToken>(cancellationToken => Task.Run(async () =>
            {
                do
                {
                    await Task.Delay(10);
                } while (!cancellationToken.IsCancellationRequested);
            }, cancellationToken));

        var cancellationTokenSource = new CancellationTokenSource();

        var hostApplicationLifetimeMock = new Mock<IHostApplicationLifetime>();
        var serviceCollection = new ServiceCollection();
        serviceCollection.AddSingleton(simpleLoopMock.Object);

        var serviceProvider = serviceCollection.BuildServiceProvider();
        var sut = new SimpleLoopsBackgroundService(
            serviceProvider,
            hostApplicationLifetimeMock.Object,
            loggerMock.Object);

        // Act
        await sut.StartAsync(cancellationTokenSource.Token);
        await Task.Delay(10);
        cancellationTokenSource.Cancel();
        await sut.ExecuteTask;

        // Assert
        simpleLoopMock.Verify(x => x.RunAsync(It.IsAny<CancellationToken>()), Times.Once);
        hostApplicationLifetimeMock.Verify(x => x.StopApplication(), Times.Once);
    }

    [Test]
    public async Task Should_Stop_When_A_Loop_Failed()
    {
        // Arrange
        var simpleLoopMock = new Mock<ISimpleLoop>();
        simpleLoopMock.Setup(x => x.RunAsync(It.IsAny<CancellationToken>()))
            .Returns(Task.FromException(new Exception()));

        var hostApplicationLifetimeMock = new Mock<IHostApplicationLifetime>();
        var serviceCollection = new ServiceCollection();
        serviceCollection.AddSingleton(simpleLoopMock.Object);

        var serviceProvider = serviceCollection.BuildServiceProvider();
        var sut = new SimpleLoopsBackgroundService(
            serviceProvider,
            hostApplicationLifetimeMock.Object,
            loggerMock.Object);

        // Act
        await sut.StartAsync(CancellationToken.None);
        await sut.ExecuteTask;

        // Assert
        simpleLoopMock.Verify(x => x.RunAsync(It.IsAny<CancellationToken>()), Times.Once);
        hostApplicationLifetimeMock.Verify(x => x.StopApplication(), Times.Once);
    }

    [Test]
    public async Task Should_Stop_When_One_Loop_Throw_Exception_And_Stop_The_Others()
    {
        // Arrange
        var simpleLoop1Mock = new Mock<ISimpleLoop>();
        simpleLoop1Mock.Setup(x => x.RunAsync(It.IsAny<CancellationToken>()))
            .Returns<CancellationToken>(cancellationToken => Task.Run(async () =>
            {
                do
                {
                    await Task.Delay(10);
                } while (!cancellationToken.IsCancellationRequested);
            }, cancellationToken));

        var simpleLoop2Mock = new Mock<ISimpleLoop>();
        simpleLoop2Mock.Setup(x => x.RunAsync(It.IsAny<CancellationToken>()))
            .Returns(Task.FromException(new Exception()));

        var hostApplicationLifetimeMock = new Mock<IHostApplicationLifetime>();
        var serviceCollection = new ServiceCollection();
        serviceCollection.AddSingleton(simpleLoop1Mock.Object);
        serviceCollection.AddSingleton(simpleLoop2Mock.Object);

        var serviceProvider = serviceCollection.BuildServiceProvider();
        var sut = new SimpleLoopsBackgroundService(
            serviceProvider,
            hostApplicationLifetimeMock.Object,
            loggerMock.Object);

        // Act
        await sut.StartAsync(CancellationToken.None);
        await sut.ExecuteTask;

        // Assert
        simpleLoop1Mock.Verify(x => x.RunAsync(It.IsAny<CancellationToken>()), Times.Once);
        simpleLoop2Mock.Verify(x => x.RunAsync(It.IsAny<CancellationToken>()), Times.Once);
        hostApplicationLifetimeMock.Verify(x => x.StopApplication(), Times.Once);
    }

    [Test]
    public async Task Should_Run_Loop_Until_Terminates()
    {
        // Arrange
        var simpleLoopMock = new Mock<ISimpleLoop>();

        var hostApplicationLifetimeMock = new Mock<IHostApplicationLifetime>();
        var serviceCollection = new ServiceCollection();
        serviceCollection.AddSingleton(simpleLoopMock.Object);

        var serviceProvider = serviceCollection.BuildServiceProvider();
        var sut = new SimpleLoopsBackgroundService(
            serviceProvider,
            hostApplicationLifetimeMock.Object,
            loggerMock.Object);

        // Act
        await sut.StartAsync(CancellationToken.None);
        await sut.ExecuteTask;
        // Assert
        simpleLoopMock.Verify(x => x.RunAsync(It.IsAny<CancellationToken>()), Times.Once);
        hostApplicationLifetimeMock.Verify(x => x.StopApplication(), Times.Once);
    }
}
