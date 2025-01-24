using Microsoft.Extensions.Logging;
using Moq;
using Sb.Common.Wrappers;

namespace Sb.SimpleLoops.Tests.Unit;

public class SimpleLoopTests
{
    private readonly Mock<ILogger<SimpleLoop<ISimpleLoopIterationExecutor>>> loggerMock;
    private readonly Mock<SimpleLoopConfiguration<ISimpleLoopIterationExecutor>> configurationMock;
    private readonly Mock<IDateTimeWrapper> dateTimeWrapperMock;

    public SimpleLoopTests()
    {
        loggerMock = new Mock<ILogger<SimpleLoop<ISimpleLoopIterationExecutor>>>();
        configurationMock = new Mock<SimpleLoopConfiguration<ISimpleLoopIterationExecutor>>();
        dateTimeWrapperMock = new Mock<IDateTimeWrapper>();
    }

    [Test]
    public async Task Should_Stop_Execution_When_CancellationToken_Cancelled()
    {
        // Arrange
        var cancellationTokenSource = new CancellationTokenSource();
        var cancellationToken = cancellationTokenSource.Token;
        cancellationTokenSource.Cancel();

        var taskDelayWrapperMock = new Mock<ITaskDelayWrapper>();
        var iterationExecutorMock = new Mock<ISimpleLoopIterationExecutor>();
        var sut = new SimpleLoop<ISimpleLoopIterationExecutor>(
            loggerMock.Object, 
            configurationMock.Object, 
            iterationExecutorMock.Object, 
            taskDelayWrapperMock.Object,
            dateTimeWrapperMock.Object);

        // Act
        await sut.RunAsync(cancellationToken);

        // Assert
        iterationExecutorMock.Verify(x => x.RunAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Test]
    public async Task Should_Continue_Execution_Without_Waiting_When_IterationExecutor_Returns_True()
    {
        // Arrange
        var cancellationTokenSource = new CancellationTokenSource();
        var cancellationToken = cancellationTokenSource.Token;

        var taskDelayWrapperMock = new Mock<ITaskDelayWrapper>();
        var iterationExecutorMock = new Mock<ISimpleLoopIterationExecutor>();
        iterationExecutorMock.Setup(x => x.RunAsync(It.IsAny<CancellationToken>()))
                            .ReturnsAsync(true)
                            .Callback(()=> cancellationTokenSource.Cancel());
        var sut = new SimpleLoop<ISimpleLoopIterationExecutor>(
            loggerMock.Object,
            configurationMock.Object,
            iterationExecutorMock.Object,
            taskDelayWrapperMock.Object,
            dateTimeWrapperMock.Object);

        // Act
        await sut.RunAsync(cancellationToken);

        // Assert
        iterationExecutorMock.Verify(x => x.RunAsync(It.IsAny<CancellationToken>()), Times.Once);
        taskDelayWrapperMock.Verify(x => x.DelayAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Test]
    public async Task Should_Wait_Between_Iterations_When_IterationExecutor_Returns_False()
    {
        // Arrange
        var cancellationTokenSource = new CancellationTokenSource();
        var cancellationToken = cancellationTokenSource.Token;

        var taskDelayWrapperMock = new Mock<ITaskDelayWrapper>();
        var iterationExecutorMock = new Mock<ISimpleLoopIterationExecutor>();
        iterationExecutorMock.Setup(x => x.RunAsync(It.IsAny<CancellationToken>()))
                            .ReturnsAsync(false)
                            .Callback(() => cancellationTokenSource.Cancel());
        var sut = new SimpleLoop<ISimpleLoopIterationExecutor>(
            loggerMock.Object,
            configurationMock.Object,
            iterationExecutorMock.Object,
            taskDelayWrapperMock.Object,
            dateTimeWrapperMock.Object);

        // Act
        await sut.RunAsync(cancellationToken);

        // Assert
        iterationExecutorMock.Verify(x => x.RunAsync(It.IsAny<CancellationToken>()), Times.Once);
        taskDelayWrapperMock.Verify(x => x.DelayAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Test]
    public void Should_Throw_Exception_When_PropagateException_True()
    {
        // Arrange
        var cancellationTokenSource = new CancellationTokenSource();
        var cancellationToken = cancellationTokenSource.Token;

        var taskDelayWrapperMock = new Mock<ITaskDelayWrapper>();
        var iterationExecutorMock = new Mock<ISimpleLoopIterationExecutor>();
        iterationExecutorMock.Setup(x => x.RunAsync(It.IsAny<CancellationToken>()))
                            .Callback(() => throw new Exception());

        var configuration = new SimpleLoopConfiguration<ISimpleLoopIterationExecutor>
        {
            PropagateException = true
        };
        var sut = new SimpleLoop<ISimpleLoopIterationExecutor>(
            loggerMock.Object,
            configuration,
            iterationExecutorMock.Object,
            taskDelayWrapperMock.Object,
            dateTimeWrapperMock.Object);

        // Act
        Assert.ThrowsAsync<Exception>(() => sut.RunAsync(cancellationToken));
    }
}
