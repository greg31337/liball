namespace xml;

/// <summary>
/// Identifies the state of a parser or reader.
/// </summary>
public enum XmlParserStatus
{
    /// <summary>
    /// Is ready to start.
    /// </summary>
    Ready = 0,
    /// <summary>
    /// Is processing input.
    /// </summary>
    Running = 1,
    /// <summary>
    /// Needs more input.
    /// </summary>
    NeedInput = 2,
    /// <summary>
    /// Is paused by the caller.
    /// </summary>
    Paused = 3,
    /// <summary>
    /// Completed successfully.
    /// </summary>
    Completed = 4,
    /// <summary>
    /// Was aborted by the caller.
    /// </summary>
    Aborted = 5,
    /// <summary>
    /// Stopped because of an error.
    /// </summary>
    Error = 6,
    /// <summary>
    /// Is closed.
    /// </summary>
    Closed = 7
}
