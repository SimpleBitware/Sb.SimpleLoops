namespace Sb.SimpleLoops;

public record SimpleLoopConfiguration<T>
{
    /// <summary>
    /// Wait time between iterations in milliseconds.
    /// </summary>
    public int WaitingTimeInMs { get; set; }

    /// <summary>
    /// If true, exceptions will be propagated to the caller.
    /// </summary>
    public bool PropagateException { get; set; }
}
