namespace log;

/// <summary>
/// Specifies what an asynchronous sink does when its bounded queue is full.
/// </summary>
public enum LogBackpressureMode
{
    /// <summary>
    /// Makes room by forwarding the oldest queued event before accepting the new event.
    /// </summary>
    Block = 0,
    /// <summary>
    /// Discards the incoming event when the queue is full.
    /// </summary>
    DropNewest = 1,
    /// <summary>
    /// Discards the oldest queued event and enqueues the incoming event.
    /// </summary>
    DropOldest = 2
}
