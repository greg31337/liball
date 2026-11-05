namespace xml;

/// <summary>
/// Specifies the action requested by an event handler.
/// </summary>
public enum XmlHandlerAction
{
    /// <summary>
    /// Continues processing.
    /// </summary>
    Continue = 0,
    /// <summary>
    /// Pauses processing.
    /// </summary>
    Pause = 1,
    /// <summary>
    /// Aborts processing.
    /// </summary>
    Abort = 2
}
