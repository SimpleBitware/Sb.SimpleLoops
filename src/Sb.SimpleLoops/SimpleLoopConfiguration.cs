namespace Sb.SimpleLoops;

/// <summary>
/// Simple loop configuration.
/// </summary>
/// <typeparam name="T">The time of simple loop.</typeparam>
public record SimpleLoopConfiguration<T>
{
    /// <summary>
    /// Wait time between iterations in milliseconds.
    /// Default is 15 seconds.
    /// </summary>
    public int WaitingTimeInMs { get; set; } = 15000;

    /// <summary>
    /// If true, loop exits and exceptions will be propagated to the caller.
    /// Default is false. All exceptions will be logged and loop will continue execution.
    /// </summary>
    public bool PropagateExceptions { get; set; }
}
