namespace Sbw.SimpleLoops;

public record SimpleLoopConfiguration
{
    /// <summary>
    /// Wait time in milliseconds between iterations.
    /// </summary>
    public int WaitingTimeInMs { get; set; }

    /// <summary>
    /// If true, exceptions will be propagated to the caller.
    /// </summary>
    public bool PropagateException { get; set; }
}
