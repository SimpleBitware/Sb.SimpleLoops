using Microsoft.Extensions.Logging;
using Moq;
using SimpleBitware.Common.Abstractions;

namespace SimpleBitware.AsyncLoops.Tests.Unit;

public class AsyncLoopTests
{
    private Mock<ILogger<AsyncLoop<IAsyncLoopIterationExecutor>>> loggerMock;
    private Mock<AsyncLoopConfiguration<IAsyncLoopIterationExecutor>> configurationMock;
    private Mock<IDateTime> dateTimeMock;

    [OneTimeSetUp]
    public void OneTimeSetUp()
    {
        loggerMock = new Mock<ILogger<AsyncLoop<IAsyncLoopIterationExecutor>>>();
        configurationMock = new Mock<AsyncLoopConfiguration<IAsyncLoopIterationExecutor>>();
        dateTimeMock = new Mock<IDateTime>();
    }

    [Test]
    public async Task Should_Stop_Execution_When_CancellationToken_Cancelled()
    {
        // Arrange
        var cancellationTokenSource = new CancellationTokenSource();
        var cancellationToken = cancellationTokenSource.Token;
        cancellationTokenSource.Cancel();

        var taskMock = new Mock<ITask>();
        var iterationExecutorMock = new Mock<IAsyncLoopIterationExecutor>();
        var sut = new AsyncLoop<IAsyncLoopIterationExecutor>(
            loggerMock.Object, 
            configurationMock.Object, 
            iterationExecutorMock.Object, 
            taskMock.Object,
            dateTimeMock.Object);

        // Act
        await sut.RunAsync(cancellationToken);

        // Assert
        iterationExecutorMock.Verify(x => x.RunAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Test]
    public async Task Should_Continue_Execution_Without_Waiting_When_IterationExecutor_Returns_Continue()
    {
        // Arrange
        var cancellationTokenSource = new CancellationTokenSource();
        var cancellationToken = cancellationTokenSource.Token;

        var taskMock = new Mock<ITask>();
        var iterationExecutorMock = new Mock<IAsyncLoopIterationExecutor>();
        iterationExecutorMock.Setup(x => x.RunAsync(It.IsAny<CancellationToken>()))
                            .ReturnsAsync(IterationResult.Continue)
                            .Callback(()=> cancellationTokenSource.Cancel());

        var sut = new AsyncLoop<IAsyncLoopIterationExecutor>(
            loggerMock.Object,
            configurationMock.Object,
            iterationExecutorMock.Object,
            taskMock.Object,
            dateTimeMock.Object);

        // Act
        await sut.RunAsync(cancellationToken);

        // Assert
        iterationExecutorMock.Verify(x => x.RunAsync(It.IsAny<CancellationToken>()), Times.Once);
        taskMock.Verify(x => x.Delay(It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Test]
    public async Task Should_Wait_Between_Iterations_When_IterationExecutor_Returns_Wait()
    {
        // Arrange
        var cancellationTokenSource = new CancellationTokenSource();
        var cancellationToken = cancellationTokenSource.Token;

        var taskMock = new Mock<ITask>();
        var iterationExecutorMock = new Mock<IAsyncLoopIterationExecutor>();
        iterationExecutorMock.Setup(x => x.RunAsync(It.IsAny<CancellationToken>()))
                            .ReturnsAsync(IterationResult.Wait)
                            .Callback(() => cancellationTokenSource.Cancel());

        var sut = new AsyncLoop<IAsyncLoopIterationExecutor>(
            loggerMock.Object,
            configurationMock.Object,
            iterationExecutorMock.Object,
            taskMock.Object,
            dateTimeMock.Object);

        // Act
        await sut.RunAsync(cancellationToken);

        // Assert
        iterationExecutorMock.Verify(x => x.RunAsync(It.IsAny<CancellationToken>()), Times.Once);
        taskMock.Verify(x => x.Delay(It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Test]
    public async Task Should_Exit_Loop_When_IterationExecutor_Returns_Stop()
    {
        // Arrange
        var cancellationTokenSource = new CancellationTokenSource();
        var cancellationToken = cancellationTokenSource.Token;

        var taskMock = new Mock<ITask>();
        var iterationExecutorMock = new Mock<IAsyncLoopIterationExecutor>();
        iterationExecutorMock.Setup(x => x.RunAsync(It.IsAny<CancellationToken>()))
                            .ReturnsAsync(IterationResult.Stop);

        var sut = new AsyncLoop<IAsyncLoopIterationExecutor>(
            loggerMock.Object,
            configurationMock.Object,
            iterationExecutorMock.Object,
            taskMock.Object,
            dateTimeMock.Object);

        // Act
        await sut.RunAsync(cancellationToken);

        // Assert
        iterationExecutorMock.Verify(x => x.RunAsync(It.IsAny<CancellationToken>()), Times.Once);
        taskMock.Verify(x => x.Delay(It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Test]
    public void Should_Throw_Exception_When_PropagateException_True()
    {
        // Arrange
        var cancellationTokenSource = new CancellationTokenSource();
        var cancellationToken = cancellationTokenSource.Token;

        var taskMock = new Mock<ITask>();
        var iterationExecutorMock = new Mock<IAsyncLoopIterationExecutor>();
        iterationExecutorMock.Setup(x => x.RunAsync(It.IsAny<CancellationToken>()))
                            .Returns(Task.FromException<IterationResult>(new Exception()));

        var configuration = new AsyncLoopConfiguration<IAsyncLoopIterationExecutor>
        {
            PropagateExceptions = true
        };
        var sut = new AsyncLoop<IAsyncLoopIterationExecutor>(
            loggerMock.Object,
            configuration,
            iterationExecutorMock.Object,
            taskMock.Object,
            dateTimeMock.Object);

        // Act
        Assert.ThrowsAsync<Exception>(() => sut.RunAsync(cancellationToken));
    }

    [Test]
    public async Task Should_Wait_Between_Iterations_When_Iterator_Throws_Exception_And_PropagateException_False()
    {
        // Arrange
        var cancellationTokenSource = new CancellationTokenSource();
        var cancellationToken = cancellationTokenSource.Token;

        var taskMock = new Mock<ITask>();
        var iterationExecutorMock = new Mock<IAsyncLoopIterationExecutor>();
        iterationExecutorMock.Setup(x => x.RunAsync(It.IsAny<CancellationToken>()))
                            .Returns(Task.FromException<IterationResult>(new Exception()))
                            .Callback(() => cancellationTokenSource.Cancel());

        var configuration = new AsyncLoopConfiguration<IAsyncLoopIterationExecutor>
        {
            PropagateExceptions = false
        };
        var sut = new AsyncLoop<IAsyncLoopIterationExecutor>(
            loggerMock.Object,
            configuration,
            iterationExecutorMock.Object,
            taskMock.Object,
            dateTimeMock.Object);

        // Act
        await sut.RunAsync(cancellationToken);

        // Assert
        iterationExecutorMock.Verify(x => x.RunAsync(It.IsAny<CancellationToken>()), Times.Once);
        taskMock.Verify(x => x.Delay(It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Once);
    }
}
