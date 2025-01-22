namespace Sbw.Wrappers;

public class TaskDelayWrapper : ITaskDelayWrapper
{
    public Task DelayAsync(int millis, CancellationToken cancellationToken)
    {
        return Task.Delay(millis, cancellationToken);
    }
}
