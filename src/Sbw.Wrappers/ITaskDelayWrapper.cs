namespace Sbw.Wrappers;

public interface ITaskDelayWrapper
{
    Task DelayAsync(int millis, CancellationToken cancellationToken);
}
