namespace Sb.SimpleLoops;

/// <summary>
/// Simple loop configuration.
/// </summary>
/// <typeparam name="T">The time of simple loop.</typeparam>
public record SimpleLoopConfiguration<T>
{
    /// <summary>
    /// Wait time between iterations in milliseconds.
    /// </summary>
    public int WaitingTimeInMs { get; set; } = 1000;

    /// <summary>
    /// If true, exceptions will be propagated to the caller.
    /// </summary>
    public bool PropagateExceptions { get; set; }
}
